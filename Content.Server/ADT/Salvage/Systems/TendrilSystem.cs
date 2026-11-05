using System.Numerics;
using Content.Server.ADT.Lavaland.DayNight;
using Content.Server.ADT.Salvage.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Parallax;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Explosion;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class TendrilSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BiomeSystem _biome = default!;
    [Dependency] private ADTLavalandDayNightSystem _dayNight = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private const string TargetKey = "Target";
    private const string TargetCoordinatesKey = "TargetCoordinates";
    private const string AggroVisionRadiusKey = "AggroVisionRadius";

    private const int AttackerSearchDepth = 5;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private readonly List<(Vector2i Index, Tile Tile)> _reserved = new();
    private readonly HashSet<Entity<ADTMineralComponent>> _minerals = new();

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TendrilComponent, MapInitEvent>(OnTendrilMapInit);
        SubscribeLocalEvent<TendrilComponent, DestructionEventArgs>(OnTendrilDestruction);
        SubscribeLocalEvent<TendrilComponent, DamageChangedEvent>(OnTendrilDamaged);
        SubscribeLocalEvent<TendrilComponent, AttackedEvent>(OnTendrilAttacked);
        SubscribeLocalEvent<TendrilComponent, BeforeDamageChangedEvent>(OnTendrilBeforeDamage);
        SubscribeLocalEvent<TendrilComponent, ComponentShutdown>(OnTendrilShutdown);

        SubscribeLocalEvent<TendrilMobComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<TendrilMobComponent, ComponentShutdown>(OnMobShutdown);

        SubscribeLocalEvent<TendrilCollapseComponent, MapInitEvent>(OnCollapseMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;

        var tendrils = EntityQueryEnumerator<TendrilComponent, TransformComponent>();
        while (tendrils.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Aggressor is { } aggressor && (now > comp.AggroEndTime || !IsValidTarget(aggressor)))
                ClearAggro((uid, comp));

            TrySpawn((uid, comp, xform), now);
        }

        var collapses = EntityQueryEnumerator<TendrilCollapseComponent>();
        while (collapses.MoveNext(out var uid, out var collapse))
        {
            if (now >= collapse.CollapseAt)
                Collapse((uid, collapse));
        }
    }

    private void TrySpawn(Entity<TendrilComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now < ent.Comp1.NextSpawn || ent.Comp1.Spawns.Count == 0)
            return;

        var maxSpawns = ent.Comp1.MaxSpawns;
        var delay = ent.Comp1.SpawnDelay;

        if (_dayNight.TryGetNight(ent.Comp2.MapUid, out var night))
        {
            maxSpawns += night.TendrilExtraSpawns;
            delay *= night.TendrilDelayMultiplier;
        }

        if (ent.Comp1.Mobs.Count >= maxSpawns)
            return;

        ent.Comp1.NextSpawn = now + delay;

        var mob = Spawn(_random.Pick(ent.Comp1.Spawns), ent.Comp2.Coordinates);
        var mobComp = EnsureComp<TendrilMobComponent>(mob);
        mobComp.Tendril = ent.Owner;
        ent.Comp1.Mobs.Add(mob);

        _popup.PopupEntity(Loc.GetString(ent.Comp1.SpawnPopup, ("mob", mob), ("tendril", ent.Owner)), ent.Owner, PopupType.SmallCaution);

        if (ent.Comp1.Aggressor is { } current)
            AggroMob((mob, mobComp), current, ent.Comp1);
    }

    private void OnTendrilMapInit(Entity<TendrilComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime;
        ClearSurroundings(ent);
    }

    private void ClearSurroundings(Entity<TendrilComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var radius = ent.Comp.ClearRadius;
        var bounds = new Box2(center.X - radius, center.Y - radius, center.X + radius + 1, center.Y + radius + 1);

        _reserved.Clear();
        _biome.ReserveTiles(gridUid, bounds.Enlarged(-0.1f), _reserved);

        _minerals.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, bounds.Enlarged(-0.1f), _minerals, LookupFlags.Static);
        foreach (var mineral in _minerals)
        {
            QueueDel(mineral);
        }
    }

    private void OnTendrilShutdown(Entity<TendrilComponent> ent, ref ComponentShutdown args)
    {
        foreach (var mob in ent.Comp.Mobs)
        {
            if (TryComp<TendrilMobComponent>(mob, out var mobComp))
                mobComp.Tendril = null;
        }

        ent.Comp.Mobs.Clear();
    }

    private void OnTendrilDestruction(Entity<TendrilComponent> ent, ref DestructionEventArgs args)
    {
        var coords = _transform.GetMapCoordinates(ent);

        Spawn(ent.Comp.Collapse, coords);
        Spawn(ent.Comp.Chest, coords);
    }

    private void OnCollapseMapInit(Entity<TendrilCollapseComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.CollapseAt = _timing.CurTime + ent.Comp.Delay;

        _popup.PopupEntity(Loc.GetString(ent.Comp.StartPopup), ent.Owner, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString(ent.Comp.TearPopup), ent.Owner, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.StartSound, ent.Owner);
    }

    private void Collapse(Entity<TendrilCollapseComponent> ent)
    {
        var xform = Transform(ent);
        var mapCoords = _transform.GetMapCoordinates(ent, xform);

        RaiseNetworkEvent(new ADTScreenShakeEvent(mapCoords, ent.Comp.ShakeRange, ent.Comp.ShakeDuration, ent.Comp.ShakeStrength));
        _audio.PlayPvs(ent.Comp.CollapseSound, xform.Coordinates);
        _popup.PopupCoordinates(Loc.GetString(ent.Comp.CollapsePopup), xform.Coordinates, PopupType.LargeCaution);

        if (xform.GridUid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var grid))
        {
            var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
            var radius = ent.Comp.Radius;

            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var indices = center + new Vector2i(dx, dy);
                    if (!_map.TryGetTileRef(gridUid, grid, indices, out var tileRef) || tileRef.Tile.IsEmpty)
                        continue;

                    if (_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                        continue;

                    Spawn(ent.Comp.Chasm, _map.GridTileToLocal(gridUid, grid, indices));
                }
            }
        }

        QueueDel(ent);
    }

    private void OnTendrilDamaged(Entity<TendrilComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin is not { } origin)
            return;

        Aggro(ent, origin);
    }

    private void OnTendrilBeforeDamage(Entity<TendrilComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin is not { } origin || !HasComp<ADTTribeMemberComponent>(origin))
            return;

        args.Cancelled = true;
        _popup.PopupEntity(Loc.GetString("adt-ashwalker-tendril-sacred"), ent.Owner, origin);
    }

    private void OnTendrilAttacked(Entity<TendrilComponent> ent, ref AttackedEvent args)
    {
        Aggro(ent, args.User);
    }

    private void OnMobState(Entity<TendrilMobComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Release(ent);
    }

    private void OnMobShutdown(Entity<TendrilMobComponent> ent, ref ComponentShutdown args)
    {
        Release(ent);
    }

    private void Release(Entity<TendrilMobComponent> ent)
    {
        if (ent.Comp.Tendril is not { } tendril)
            return;

        ent.Comp.Tendril = null;

        if (TryComp<TendrilComponent>(tendril, out var comp))
            comp.Mobs.Remove(ent);
    }

    public void Aggro(Entity<TendrilComponent> ent, EntityUid origin)
    {
        if (ResolveAttacker(origin) is not { } attacker || !IsValidTarget(attacker))
            return;

        if (ent.Comp.Aggressor is { } previous && previous != attacker)
            ClearAggro(ent);

        ent.Comp.Aggressor = attacker;
        ent.Comp.AggroEndTime = _timing.CurTime + ent.Comp.AggroMemory;

        foreach (var mob in ent.Comp.Mobs)
        {
            if (TryComp<TendrilMobComponent>(mob, out var mobComp))
                AggroMob((mob, mobComp), attacker, ent.Comp);
        }
    }

    private void AggroMob(Entity<TendrilMobComponent> mob, EntityUid target, TendrilComponent tendril)
    {
        if (TerminatingOrDeleted(mob) || mob.Owner == target || _mobState.IsDead(mob))
            return;

        if (_faction.IsEntityFriendly(mob.Owner, target))
            return;

        if (GetDistance(mob, target) is not { } distance)
            return;

        _faction.AggroEntity(mob.Owner, target);

        if (!TryComp<HTNComponent>(mob, out var htn))
            return;

        _npc.WakeNPC(mob, htn);

        var blackboard = htn.Blackboard;

        mob.Comp.BaseAggroRadius ??= blackboard.GetValueOrDefault<float>(AggroVisionRadiusKey, EntityManager);

        var wanted = MathF.Min(distance + tendril.AggroRadiusPadding, tendril.MaxAggroRadius);
        blackboard.SetValue(AggroVisionRadiusKey, MathF.Max(mob.Comp.BaseAggroRadius.Value, wanted));

        if (HasLiveTarget(blackboard))
            return;

        blackboard.SetValue(TargetKey, target);
        blackboard.SetValue(TargetCoordinatesKey, new EntityCoordinates(target, Vector2.Zero));
    }

    private void ClearAggro(Entity<TendrilComponent> ent)
    {
        if (ent.Comp.Aggressor is { } aggressor)
        {
            foreach (var mob in ent.Comp.Mobs)
            {
                if (TerminatingOrDeleted(mob))
                    continue;

                _faction.DeAggroEntity(mob, aggressor);

                if (!TryComp<TendrilMobComponent>(mob, out var mobComp))
                    continue;

                if (TryComp<HTNComponent>(mob, out var htn))
                {
                    htn.Blackboard.Remove<EntityUid>(TargetKey);
                    htn.Blackboard.Remove<EntityCoordinates>(TargetCoordinatesKey);

                    if (mobComp.BaseAggroRadius is { } baseRadius)
                        htn.Blackboard.SetValue(AggroVisionRadiusKey, baseRadius);
                }

                mobComp.BaseAggroRadius = null;
            }
        }

        ent.Comp.Aggressor = null;
    }

    private EntityUid? ResolveAttacker(EntityUid origin)
    {
        var current = origin;

        for (var depth = 0; depth < AttackerSearchDepth; depth++)
        {
            if (HasComp<MobStateComponent>(current))
                return current;

            var parent = Transform(current).ParentUid;
            if (!parent.IsValid())
                return null;

            current = parent;
        }

        return null;
    }

    private bool IsValidTarget(EntityUid target)
    {
        if (TerminatingOrDeleted(target) || !HasComp<MobStateComponent>(target))
            return false;

        return !_mobState.IsDead(target);
    }

    private bool HasLiveTarget(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var current, EntityManager))
            return false;

        if (!current.IsValid() || TerminatingOrDeleted(current))
            return false;

        return !_mobState.IsDead(current);
    }

    private float? GetDistance(EntityUid uid, EntityUid target)
    {
        var ourCoords = _transform.GetMapCoordinates(uid);
        var targetCoords = _transform.GetMapCoordinates(target);

        if (ourCoords.MapId == MapId.Nullspace || ourCoords.MapId != targetCoords.MapId)
            return null;

        return (ourCoords.Position - targetCoords.Position).Length();
    }
}
