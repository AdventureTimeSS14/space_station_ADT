using System.Linq;
using Content.Server.ADT.Drake;
using Content.Server.ADT.Mining;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Destructible;
using Content.Shared.ADT.ArmorPenetration;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Vetus;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Station;
using Content.Shared.Damage.Events;
using Content.Shared.Wall;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem : EntitySystem
{
    [Dependency] private ADTDrakeEffectsSystem _drakeEffects = default!;
    [Dependency] private ADTMineralSystem _mineral = default!;
    [Dependency] private ArmorPenetrationSystem _penetration = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";

    private static readonly Vector2i[] CardinalOffsets =
    {
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
    };

    private static readonly Vector2i[] AllOffsets =
    {
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1),
    };

    private readonly HashSet<Entity<HumanoidProfileComponent>> _humanoids = new();

    private DamageModifierSet? _blockAll;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVetusComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTVetusComponent, ProjectileReflectAttemptEvent>(OnProjectileReflectAttempt);
        SubscribeLocalEvent<ADTVetusComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ADTVetusComponent, BeforeExplodeEvent>(OnBeforeExplode);
        SubscribeLocalEvent<ADTVetusComponent, ElectrocutionAttemptEvent>(OnElectrocutionAttempt);
        SubscribeLocalEvent<ADTVetusComponent, ADTMegafaunaAttemptOpenFireEvent>(OnAttemptOpenFire);
        SubscribeLocalEvent<ADTVetusComponent, ADTMegafaunaOpenFireEvent>(OnOpenFire);
        SubscribeLocalEvent<ADTVetusComponent, ADTMegafaunaDevourEvent>(OnDevour);

        SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanDamageAttempt);
        SubscribeLocalEvent<HitscanStaminaDamageComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanStaminaAttempt);

        InitializeCharge();
        InitializeLegs();
        InitializeAnomalies();
        InitializeTesla();
        InitializeSelfDestruct();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        UpdateLegs(now);
        UpdateAnomalies(now);
        UpdateTesla(now);
    }

    private void OnMapInit(Entity<ADTVetusComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Modes.Count > 0)
            ent.Comp.Mode = _random.Pick(ent.Comp.Modes);

        if (ent.Comp.Mode is { } mode)
            _appearance.SetData(ent, ADTVetusVisuals.Mode, mode);

        if (ent.Comp.Mode == ADTVetusMode.Flux && TryComp<MeleeWeaponComponent>(ent, out var melee))
        {
            melee.Damage = new DamageSpecifier(_proto.Index(HeatType), ent.Comp.FluxMeleeDamage);
            Dirty(ent, melee);
        }

        SetShield(ent, true);
    }

    private void SetShield(Entity<ADTVetusComponent> ent, bool enabled)
    {
        ent.Comp.ShieldEnabled = enabled;
        _appearance.SetData(ent, ADTVetusVisuals.Shield, enabled);
    }

    private void RestoreShield(Entity<ADTVetusComponent> ent)
    {
        if (ent.Comp.ShieldEnabled || _mobState.IsDead(ent))
            return;

        SetShield(ent, true);
        _popup.PopupEntity(Loc.GetString("adt-vetus-shield-up", ("vetus", ent.Owner)), ent, PopupType.MediumCaution);
    }

    private void DisableShield(Entity<ADTVetusComponent> ent)
    {
        _popup.PopupEntity(Loc.GetString("adt-vetus-shield-down", ("vetus", ent.Owner)), ent, PopupType.MediumCaution);
        SetShield(ent, false);
        _megafauna.Schedule(ent, ent.Comp.ShieldCooldown, () => RestoreShield(ent));
    }

    private void ShieldSparks(Entity<ADTVetusComponent> ent)
    {
        Spawn(ent.Comp.ShieldSparks, _transform.GetMapCoordinates(ent));
        _audio.PlayPvs(ent.Comp.ShieldBlockSound, ent);
    }

    private void OnProjectileReflectAttempt(Entity<ADTVetusComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.ShieldEnabled)
            return;

        args.Cancelled = true;
        QueueDel(args.ProjUid);

        ShieldSparks(ent);
        _popup.PopupEntity(Loc.GetString("adt-vetus-shield-deflect-projectile", ("vetus", ent.Owner)), ent, PopupType.MediumCaution);

        if (args.Component.Damage.GetTotal() > 0 || HasComp<StaminaDamageOnCollideComponent>(args.ProjUid))
            DisableShield(ent);
    }

    private void OnHitscanDamageAttempt(Entity<HitscanBasicDamageComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        TryBlockHitscan(ref args, ent.Comp.Damage.GetTotal() > 0);
    }

    private void OnHitscanStaminaAttempt(Entity<HitscanStaminaDamageComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    {
        TryBlockHitscan(ref args, ent.Comp.StaminaDamage > 0f);
    }

    private void TryBlockHitscan(ref AttemptHitscanRaycastFiredEvent args, bool damaging)
    {
        if (args.Cancelled || args.Data.HitEntity is not { } hit || !TryComp<ADTVetusComponent>(hit, out var vetus))
            return;

        if (!vetus.ShieldEnabled)
            return;

        args.Cancelled = true;

        Entity<ADTVetusComponent> ent = (hit, vetus);
        ShieldSparks(ent);
        _popup.PopupEntity(Loc.GetString("adt-vetus-shield-deflect-projectile", ("vetus", hit)), hit, PopupType.MediumCaution);

        if (damaging)
            DisableShield(ent);
    }

    private void OnAttacked(Entity<ADTVetusComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.ShieldEnabled || args.Used == args.User)
            return;

        args.ModifiersList.Add(GetBlockAllModifiers());

        ShieldSparks(ent);
        _popup.PopupEntity(Loc.GetString("adt-vetus-shield-deflect-melee", ("vetus", ent.Owner)), ent, args.User, PopupType.MediumCaution);

        if (_melee.GetDamage(args.Used, args.User).GetTotal() > 0)
            DisableShield(ent);
    }

    private DamageModifierSet GetBlockAllModifiers()
    {
        if (_blockAll != null)
            return _blockAll;

        _blockAll = new DamageModifierSet();

        foreach (var type in _proto.EnumeratePrototypes<DamageTypePrototype>())
        {
            _blockAll.Coefficients[type.ID] = 0f;
        }

        return _blockAll;
    }

    private void OnBeforeExplode(Entity<ADTVetusComponent> ent, ref BeforeExplodeEvent args)
    {
        var severity = GetExplosionSeverity(ent, ref args, ent.Comp.HeavyIntensity, ent.Comp.DevastationIntensity);
        ApplyExplosionDamage(ent, severity, ent.Comp.HeavyExplosionDamage, ent.Comp.DevastationExplosionDamage);
    }

    private int GetExplosionSeverity(EntityUid uid, ref BeforeExplodeEvent args, float heavy, float devastation, bool consume = true)
    {
        var total = (float) args.Damage.GetTotal();

        if (consume)
            args.Damage.DamageDict.Clear();

        if (!_proto.TryIndex<ExplosionPrototype>(args.Id, out var explosion))
            return 1;

        var perIntensity = (float) explosion.DamagePerIntensity.GetTotal();
        var coefficient = TryComp<ExplosionResistanceComponent>(uid, out var resistance) ? resistance.DamageCoefficient : 1f;

        if (perIntensity <= 0f || coefficient <= 0f)
            return 1;

        var intensity = total / (perIntensity * coefficient);

        if (intensity >= devastation)
            return 3;

        if (intensity >= heavy)
            return 2;

        return 1;
    }

    private void ApplyExplosionDamage(EntityUid uid, int severity, float heavyDamage, float devastationDamage)
    {
        var amount = severity switch
        {
            3 => devastationDamage,
            2 => heavyDamage,
            _ => 0f,
        };

        if (amount <= 0f)
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", amount);
        _damageable.TryChangeDamage(uid, damage, true);
    }

    private void OnElectrocutionAttempt(Entity<ADTVetusComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttemptOpenFire(Entity<ADTVetusComponent> ent, ref ADTMegafaunaAttemptOpenFireEvent args)
    {
        if (ent.Comp.Exploding || _charge.IsCharging(ent))
            args.Cancelled = true;
    }

    private void OnOpenFire(Entity<ADTVetusComponent> ent, ref ADTMegafaunaOpenFireEvent args)
    {
        if (!TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            return;

        var anger = _megafauna.CalculateAnger((ent.Owner, ai));
        var cooldown = ent.Comp.RangedCooldown * ((10 - ent.Comp.ExtraPlayerAnger) / 10.0);
        _megafauna.SetRangedCooldown((ent.Owner, ai), cooldown);

        if (_random.Prob(Math.Clamp(ent.Comp.ChargeChance + anger / 200f, 0f, 1f)))
            TripleCharge(ent, args.Target);
        else if (_random.Prob(Math.Clamp(ent.Comp.AnomalyChance + anger / 100f, 0f, 1f)))
            SpawnAnomalies(ent);
        else if (_random.Prob(Math.Clamp(ent.Comp.SpecialChance + anger / 100f, 0f, 1f)))
            DoSpecialMove(ent, args.Target);

        CalculateExtraPlayerAnger(ent);
    }

    private void CalculateExtraPlayerAnger(Entity<ADTVetusComponent> ent)
    {
        var center = _transform.GetMapCoordinates(ent);
        var anger = 0;

        _humanoids.Clear();
        _lookup.GetEntitiesInRange(center, ent.Comp.AngerRange * MathF.Sqrt(2f) + 0.5f, _humanoids);

        foreach (var humanoid in _humanoids)
        {
            if (_mobState.IsDead(humanoid) || GetTileDistance(ent, humanoid) > ent.Comp.AngerRange)
                continue;

            anger++;
        }

        var cap = IsOnStation(ent) ? ent.Comp.StationAngerCap : ent.Comp.AngerCap;
        ent.Comp.ExtraPlayerAnger = Math.Clamp(anger, 1, Math.Max(cap, 1)) - 1;
    }

    private void OnDevour(Entity<ADTVetusComponent> ent, ref ADTMegafaunaDevourEvent args)
    {
        args.Handled = true;

        var target = args.Target;

        _megafauna.Say(ent, "adt-vetus-devour", 4, true);
        _popup.PopupCoordinates(
            Loc.GetString("adt-vetus-disintegrate", ("vetus", ent.Owner), ("target", target)),
            _transform.GetMoverCoordinates(target),
            PopupType.LargeCaution);

        if ((!IsOnStation(ent) || HasComp<ActorComponent>(ent)) && _thresholds.TryGetDeadThreshold(ent, out var maxHealth))
            _damageable.HealEvenly(ent.Owner, -maxHealth.Value * ent.Comp.DevourHealFraction, origin: ent);

        _megafauna.Disintegrate(target, ent.Comp.DevourRemains);
    }

    private void DestroySurroundings(Entity<ADTVetusComponent> ent)
    {
        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var origin = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        foreach (var offset in CardinalOffsets)
        {
            SmashTile(ent, gridUid, grid, origin + offset);
        }
    }

    private void SmashTile(Entity<ADTVetusComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
        {
            if (TryComp<ADTMineralComponent>(anchored, out var mineral))
            {
                _mineral.Mine((anchored, mineral), 1, ent);
                return;
            }

            if (HasComp<WallComponent>(anchored))
            {
                _destructible.DestroyEntity(anchored);
                return;
            }
        }

        foreach (var entity in _lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid).ToList())
        {
            if (entity == ent.Owner || HasComp<MobStateComponent>(entity) || HasComp<ItemComponent>(entity))
                continue;

            if (!HasComp<DamageableComponent>(entity) || !TryComp<PhysicsComponent>(entity, out var physics) || !physics.Hard || !physics.CanCollide)
                continue;

            if (!Transform(entity).Anchored)
                continue;

            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Structural", ent.Comp.SmashObjectDamage);
            _damageable.TryChangeDamage(entity, damage, origin: ent);
            return;
        }
    }

    private bool IsOnStation(EntityUid uid)
    {
        return _station.GetOwningStation(uid) != null;
    }

    private bool TryGetTile(EntityUid uid, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        var xform = Transform(uid);

        if (xform.GridUid is not { } gridEnt || !TryComp<MapGridComponent>(gridEnt, out var gridComp))
            return false;

        gridUid = gridEnt;
        grid = gridComp;
        tile = _map.TileIndicesFor(gridEnt, gridComp, xform.Coordinates);
        return true;
    }

    private int GetTileDistance(EntityUid a, EntityUid b)
    {
        var first = _transform.GetMapCoordinates(a);
        var second = _transform.GetMapCoordinates(b);

        if (first.MapId != second.MapId)
            return int.MaxValue;

        var delta = second.Position - first.Position;
        return (int) MathF.Round(MathF.Max(MathF.Abs(delta.X), MathF.Abs(delta.Y)));
    }

    private bool IsTileFree(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        return !_turf.IsTileBlocked(gridUid, tile, Content.Shared.Physics.CollisionGroup.Impassable, grid);
    }

    private List<MapCoordinates> GetFreeTilesAround(MapCoordinates center, int range, int minRange, bool unoccluded)
    {
        var result = new List<MapCoordinates>();

        if (!_map.TryFindGridAt(center, out var gridUid, out var grid))
            return result;

        var origin = _map.TileIndicesFor(gridUid, grid, center);

        for (var x = -range; x <= range; x++)
        {
            for (var y = -range; y <= range; y++)
            {
                if (Math.Max(Math.Abs(x), Math.Abs(y)) <= minRange)
                    continue;

                var indices = origin + new Vector2i(x, y);

                if (!IsTileFree(gridUid, grid, indices))
                    continue;

                var coords = _transform.ToMapCoordinates(_map.GridTileToLocal(gridUid, grid, indices));

                if (unoccluded && !_examine.InRangeUnOccluded(center, coords, range * 1.5f, null))
                    continue;

                result.Add(coords);
            }
        }

        return result;
    }

    private EntityUid? ShootProjectile(EntityUid shooter, EntProtoId proto, MapCoordinates from, MapCoordinates to, float speed)
    {
        if (from.MapId != to.MapId || from.MapId == MapId.Nullspace)
            return null;

        var direction = to.Position - from.Position;

        if (direction.LengthSquared() < 0.01f)
            return null;

        var projectile = Spawn(proto, from);
        _gun.ShootProjectile(projectile, direction, System.Numerics.Vector2.Zero, shooter, shooter, speed);
        return projectile;
    }
}
