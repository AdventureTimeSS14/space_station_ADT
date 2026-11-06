using System.Linq;
using System.Numerics;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Destructible;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Legion;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.NPC;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Gatherable;
using Content.Shared.Gatherable.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Sprite;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using FixturesComponent = Robust.Shared.Physics.FixturesComponent;

namespace Content.Server.ADT.Legion;

public sealed class ADTLegionMegafaunaSystem : EntitySystem
{
    [Dependency] private ADTLegionGateSystem _gate = default!;
    [Dependency] private ADTLegionSkullSystem _skull = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private GatherableSystem _gatherable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedScaleVisualsSystem _scale = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const string TargetKey = "Target";
    private const string TargetCoordinatesKey = "TargetCoordinates";

    private readonly HashSet<EntityUid> _current = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLegionMegafaunaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTLegionMegafaunaComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTLegionMegafaunaComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ADTLegionMegafaunaComponent, ADTMegafaunaOpenFireEvent>(OnOpenFire);
        SubscribeLocalEvent<ADTLegionMegafaunaComponent, ADTMegafaunaAfterMeleeHitEvent>(OnAfterMeleeHit);
        SubscribeLocalEvent<ADTLegionMegafaunaComponent, ADTLegionOpenFireActionEvent>(OnOpenFireAction);
        SubscribeLocalEvent<ADTLegionMegafaunaComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLegionBeamComponent>();

        while (query.MoveNext(out var uid, out var beam))
        {
            if (now >= beam.EndsAt || TerminatingOrDeleted(beam.Caster))
            {
                QueueDel(uid);
                continue;
            }

            if (now < beam.NextTick)
                continue;

            beam.NextTick = now + GetBeamTick(beam.Caster);
            UpdateBeam(beam);
        }
    }

    private void OnMapInit(Entity<ADTLegionMegafaunaComponent> ent, ref MapInitEvent args)
    {
        UpdateScale(ent);
    }

    private void OnDamageChanged(Entity<ADTLegionMegafaunaComponent> ent, ref DamageChangedEvent args)
    {
        _gate.TryReleaseByLegion(ent.Owner);

        if (_mobState.IsDead(ent))
            return;

        ent.Comp.Shrunk = true;
        UpdateScale(ent);

        if (!args.DamageIncreased || !_random.Prob(ent.Comp.SkullChance))
            return;

        _megafauna.TryGetTarget(ent.Owner, out var target);
        SpawnMinion(ent, ent.Comp.SkullProto, target);
    }

    private void OnMobStateChanged(Entity<ADTLegionMegafaunaComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        _megafauna.ClearSchedule(ent);
        QueueDel(ent);
    }

    private void OnAfterMeleeHit(Entity<ADTLegionMegafaunaComponent> ent, ref ADTMegafaunaAfterMeleeHitEvent args)
    {
        if (args.Handled || !HasComp<HumanoidProfileComponent>(args.Target) || !_mobState.IsCritical(args.Target))
            return;

        args.Handled = true;

        if (_skull.InfestTarget(args.Target, ent.Comp.InfestProto, ent.Comp.InfestMessage, ent.Owner) is not { } legion)
            return;

        if (TryComp<NpcFactionMemberComponent>(ent, out var factions))
        {
            _faction.ClearFactions(legion, false);
            _faction.AddFactions(legion, factions.Factions);
        }
    }

    private void OnRefreshSpeed(Entity<ADTLegionMegafaunaComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.FiringLaser)
            args.ModifySpeed(0f, 0f);
    }

    private void OnOpenFire(Entity<ADTLegionMegafaunaComponent> ent, ref ADTMegafaunaOpenFireEvent args)
    {
        OpenFire(ent, args.Target, _transform.GetMapCoordinates(args.Target));
    }

    private void OnOpenFireAction(Entity<ADTLegionMegafaunaComponent> ent, ref ADTLegionOpenFireActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTMegafaunaAIComponent>(ent, out var ai) || !_megafauna.IsRangedReady((ent.Owner, ai)))
            return;

        var aim = _transform.ToMapCoordinates(args.Target);

        if (aim.MapId == MapId.Nullspace)
            return;

        args.Handled = true;
        OpenFire(ent, FindMobAt(aim), aim);
    }

    private void OpenFire(Entity<ADTLegionMegafaunaComponent> ent, EntityUid? target, MapCoordinates aim)
    {
        if (ent.Comp.Charging || ent.Comp.FiringLaser)
            return;

        if (_random.Prob(ent.Comp.ChargeChance))
        {
            Charge(ent);
            return;
        }

        if (_random.Prob(ent.Comp.LaserChance))
        {
            Laser(ent, aim);
            return;
        }

        if (_random.Prob(ent.Comp.BigLegionChance))
        {
            SpawnMinion(ent, ent.Comp.BigLegionProto, target);
            _popup.PopupEntity(Loc.GetString("adt-legion-megafauna-summon-big", ("legion", ent.Owner)), ent, PopupType.MediumCaution);
            SetCooldown(ent, ent.Comp.BigLegionCooldown);
            return;
        }

        var tendrilLegion = SpawnMinion(ent, ent.Comp.TendrilLegionProto, target);
        _popup.PopupEntity(
            Loc.GetString("adt-legion-megafauna-summon-legion", ("minion", tendrilLegion), ("legion", ent.Owner)),
            ent,
            PopupType.MediumCaution);
        SetCooldown(ent, ent.Comp.TendrilLegionCooldown);
    }

    private void Charge(Entity<ADTLegionMegafaunaComponent> ent)
    {
        _popup.PopupEntity(Loc.GetString("adt-legion-megafauna-charge", ("legion", ent.Owner)), ent, PopupType.LargeCaution);
        _appearance.SetData(ent, ADTLegionMegafaunaVisuals.SpinUntil, _timing.CurTime + ent.Comp.SpinDuration);

        ent.Comp.Charging = true;
        SetBlackboardFlag(ent, ent.Comp.ChargingKey, true);
        SetFleeEnabled(ent, false);
        SetCooldown(ent, ent.Comp.ChargeDuration);

        _megafauna.Schedule(ent, ent.Comp.ChargeDuration, () =>
        {
            ent.Comp.Charging = false;
            SetBlackboardFlag(ent, ent.Comp.ChargingKey, false);
            SetFleeEnabled(ent, true);
        });
    }

    private void Laser(Entity<ADTLegionMegafaunaComponent> ent, MapCoordinates aim)
    {
        if (GetOrigin(ent) is not { } origin || aim.MapId != origin.MapId)
            return;

        ent.Comp.FiringLaser = true;
        SetBlackboardFlag(ent, ent.Comp.FiringLaserKey, true);
        SetFleeEnabled(ent, false);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        var delta = aim.Position - origin.Position;
        var angle = delta.LengthSquared() < 0.01f ? 90f : ADTMegafaunaGeometry.VectorToByondAngle(delta);
        var direction = ADTMegafaunaGeometry.ByondAngleToVector(angle);
        var end = new Vector2(
            MathF.Round(direction.X * ent.Comp.LaserRange),
            MathF.Round(direction.Y * ent.Comp.LaserRange));
        var location = new MapCoordinates(origin.Position + end, origin.MapId);

        var beamTime = ent.Comp.LaserBaseTelegraph + ent.Comp.LaserHealthTelegraph * GetHealthFraction(ent);

        _audio.PlayPvs(ent.Comp.LaserTelegraphSound, ent);
        SpawnBeamVisual(ent.Comp.BeamTelegraphProto, origin, location, beamTime);
        SetCooldown(ent, beamTime + ent.Comp.LaserRecovery);

        _megafauna.Schedule(ent, beamTime, () => FireLaser(ent, location));
        _megafauna.Schedule(ent, beamTime + ent.Comp.LaserRecovery, () =>
        {
            ent.Comp.FiringLaser = false;
            SetBlackboardFlag(ent, ent.Comp.FiringLaserKey, false);
            SetFleeEnabled(ent, !ent.Comp.Charging);
            _speed.RefreshMovementSpeedModifiers(ent.Owner);
        });
    }

    private void FireLaser(Entity<ADTLegionMegafaunaComponent> ent, MapCoordinates location)
    {
        if (GetOrigin(ent) is not { } origin || location.MapId != origin.MapId)
            return;

        _audio.PlayPvs(ent.Comp.LaserFireSound, ent);
        SpawnBeamVisual(ent.Comp.BeamProto, origin, location, ent.Comp.LaserBeamDuration, ent.Comp.BeamLitProto, ent.Comp.BeamLightSpacing);

        if (Transform(ent).GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var startTile = _map.TileIndicesFor(gridUid, grid, origin);
        var endTile = _map.TileIndicesFor(gridUid, grid, location);
        var tiles = ADTMegafaunaGeometry.GetLine(startTile, endTile);

        var beamEnt = Spawn(null, MapCoordinates.Nullspace);
        var beam = AddComp<ADTLegionBeamComponent>(beamEnt);
        beam.Caster = ent.Owner;
        beam.Grid = gridUid;
        beam.Tiles = tiles;
        beam.EndsAt = _timing.CurTime + ent.Comp.LaserBeamDuration;
        beam.NextTick = _timing.CurTime + ent.Comp.LaserBeamTick;

        foreach (var tile in tiles)
        {
            BurnTerrain(ent, gridUid, grid, tile);
        }

        foreach (var mob in GetMobsOnTiles(gridUid, grid, tiles))
        {
            beam.Inside.Add(mob);
            HitByLaser(ent, mob, true);
        }
    }

    private void BurnTerrain(Entity<ADTLegionMegafaunaComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
        {
            if (HasComp<GatherableComponent>(anchored))
            {
                _gatherable.Gather(anchored);
                continue;
            }

            if (!_megafauna.IsWall(anchored) || _destructible.DestroyedAt(anchored) == FixedPoint2.MaxValue)
                continue;

            var wall = anchored;
            _megafauna.Schedule(ent, ent.Comp.LaserWallMeltDelay, () =>
            {
                if (!TerminatingOrDeleted(wall))
                    _destructible.DestroyEntity(wall);
            });
        }
    }

    private void UpdateBeam(ADTLegionBeamComponent beam)
    {
        if (!TryComp<ADTLegionMegafaunaComponent>(beam.Caster, out var legion)
            || !TryComp<MapGridComponent>(beam.Grid, out var grid))
            return;

        Entity<ADTLegionMegafaunaComponent> caster = (beam.Caster, legion);
        _current.Clear();

        foreach (var mob in GetMobsOnTiles(beam.Grid, grid, beam.Tiles))
        {
            _current.Add(mob);

            if (!beam.Inside.Contains(mob))
                HitByLaser(caster, mob, false);
        }

        beam.Inside.Clear();
        beam.Inside.UnionWith(_current);
    }

    private void HitByLaser(Entity<ADTLegionMegafaunaComponent> ent, EntityUid mob, bool factionFirst)
    {
        if (mob == ent.Owner || TerminatingOrDeleted(mob))
            return;

        var friendly = _faction.IsEntityFriendly(ent.Owner, mob);

        if (factionFirst && friendly)
            return;

        if (_mobState.IsDead(mob))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-megafauna-laser-dust", ("target", mob)), mob, PopupType.LargeCaution);
            _megafauna.Disintegrate(mob, ent.Comp.RemainsProto);
            return;
        }

        if (friendly)
            return;

        _audio.PlayPvs(ent.Comp.LaserHitSound, mob, AudioParams.Default.WithVolume(-6f));
        _popup.PopupEntity(Loc.GetString("adt-legion-megafauna-laser-hit"), mob, mob, PopupType.LargeCaution);

        var amount = ent.Comp.LaserDamageMax - GetHealthFraction(ent) * ent.Comp.LaserDamageHealthScale;
        var damage = new DamageSpecifier();
        damage.DamageDict.Add(ent.Comp.LaserDamageType, amount);
        _damageable.TryChangeDamage(mob, damage, origin: ent.Owner);
    }

    private IEnumerable<EntityUid> GetMobsOnTiles(EntityUid gridUid, MapGridComponent grid, List<Vector2i> tiles)
    {
        var tileSet = new HashSet<Vector2i>(tiles);
        var found = new HashSet<EntityUid>();

        foreach (var uid in _lookup.GetLocalEntitiesIntersecting(gridUid, tiles))
        {
            if (!HasComp<MobStateComponent>(uid) || found.Contains(uid))
                continue;

            var tile = _map.TileIndicesFor(gridUid, grid, Transform(uid).Coordinates);

            if (tileSet.Contains(tile))
                found.Add(uid);
        }

        return found;
    }

    private void SpawnBeamVisual(
        EntProtoId proto,
        MapCoordinates from,
        MapCoordinates to,
        TimeSpan lifetime,
        EntProtoId? litProto = null,
        int lightSpacing = 0)
    {
        var delta = to.Position - from.Position;
        var length = delta.Length();

        if (length < 0.01f)
            return;

        var direction = delta / length;
        var rotation = new Angle(MathF.Atan2(-direction.X, direction.Y));
        var segments = (int) MathF.Ceiling(length);

        for (var i = 0; i < segments; i++)
        {
            var position = from.Position + direction * MathF.Min(i + 0.5f, length);
            var segmentProto = litProto is { } lit && lightSpacing > 0 && i % lightSpacing == 0
                ? lit
                : proto;
            var segment = Spawn(segmentProto, new MapCoordinates(position, from.MapId));
            _transform.SetWorldRotation(segment, rotation);

            if (TryComp<TimedDespawnComponent>(segment, out var despawn))
                despawn.Lifetime = (float) lifetime.TotalSeconds;
        }
    }

    private EntityUid SpawnMinion(Entity<ADTLegionMegafaunaComponent> ent, EntProtoId proto, EntityUid? target)
    {
        var minion = Spawn(proto, _transform.GetMoverCoordinates(ent));

        if (TryComp<NpcFactionMemberComponent>(ent, out var factions))
        {
            _faction.ClearFactions(minion, false);
            _faction.AddFactions(minion, factions.Factions);
        }

        if (target is not { } victim || TerminatingOrDeleted(victim) || !TryComp<HTNComponent>(minion, out var htn))
            return minion;

        htn.Blackboard.SetValue(TargetKey, victim);
        htn.Blackboard.SetValue(TargetCoordinatesKey, new EntityCoordinates(victim, Vector2.Zero));
        _npc.WakeNPC(minion, htn);

        return minion;
    }

    private void UpdateScale(Entity<ADTLegionMegafaunaComponent> ent)
    {
        var scale = !ent.Comp.Shrunk && ent.Comp.InitialScale is { } initial
            ? initial
            : ent.Comp.ScaleBase + GetHealthFraction(ent);

        if (MathF.Abs(scale - ent.Comp.CurrentScale) < 0.01f)
            return;

        ent.Comp.CurrentScale = scale;
        _scale.SetSpriteScale(ent, new Vector2(scale, scale));

        if (!TryComp<FixturesComponent>(ent, out var fixtures)
            || !fixtures.Fixtures.TryGetValue(ent.Comp.HitboxFixture, out var fixture)
            || fixture.Shape is not PhysShapeCircle circle)
            return;

        _physics.SetPositionRadius(
            ent,
            ent.Comp.HitboxFixture,
            fixture,
            circle,
            ent.Comp.HitboxOffset,
            ent.Comp.HitboxRadius * scale,
            fixtures);
    }

    private float GetHealthFraction(EntityUid uid)
    {
        var max = _megafauna.GetMaxHealth(uid);

        if (max <= 0f)
            return 0f;

        return Math.Clamp(_megafauna.GetHealth(uid) / max, 0f, 1f);
    }

    private TimeSpan GetBeamTick(EntityUid caster)
    {
        return TryComp<ADTLegionMegafaunaComponent>(caster, out var legion)
            ? legion.LaserBeamTick
            : TimeSpan.FromSeconds(0.1);
    }

    private void SetCooldown(Entity<ADTLegionMegafaunaComponent> ent, TimeSpan cooldown)
    {
        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRangedCooldown((ent.Owner, ai), cooldown);
    }

    private void SetBlackboardFlag(EntityUid uid, string key, bool value)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (value)
            htn.Blackboard.SetValue(key, true);
        else
            htn.Blackboard.Remove<bool>(key);
    }

    private void SetFleeEnabled(EntityUid uid, bool enabled)
    {
        if (TryComp<ADTFleeComponent>(uid, out var flee))
            flee.Enabled = enabled;
    }

    private MapCoordinates? GetOrigin(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return null;

        var coords = _transform.GetMoverCoordinates(uid).SnapToGrid(EntityManager);
        var map = _transform.ToMapCoordinates(coords);

        return map.MapId == MapId.Nullspace ? null : map;
    }

    private EntityUid? FindMobAt(MapCoordinates coords)
    {
        foreach (var candidate in _lookup.GetEntitiesInRange<MobStateComponent>(coords, 0.75f))
        {
            if (!_mobState.IsDead(candidate.Owner))
                return candidate.Owner;
        }

        return null;
    }
}
