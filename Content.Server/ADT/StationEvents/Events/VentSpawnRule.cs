using Content.Server.Antag;
using Content.Server.ADT.StationEvents.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.ADT.VentCrawling;
using Content.Shared.ADT.VentCrawling.Components;
using Content.Shared.Eye;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Movement.Systems;
using Content.Shared.Station.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.ADT.StationEvents.Events;

public sealed partial class VentSpawnRule : StationEventSystem<VentSpawnRuleComponent>
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVentCrawableSystem _crawable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
        SubscribeLocalEvent<VentSpawnRuleComponent, AntagSelectEntityEvent>(OnSelectEntity, after: [typeof(AntagSpawnerSystem)]);
        SubscribeLocalEvent<GhostRoleSpawnerUsedEvent>(OnSpawnerUsed);
    }

    protected override void Added(EntityUid uid, VentSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        var locations = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out var vent, out _, out var transform))
        {
            if (!transform.Anchored)
                continue;

            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != station)
                continue;

            if (!IsVentUsable(vent))
                continue;

            locations.Add(transform.Coordinates);
        }

        if (locations.Count == 0)
        {
            Sawmill.Warning("VentSpawnRule found no valid vents, cannot pick location");
            ForceEndSelf(uid, gameRule);
            return;
        }

        var spawnLocation = _random.Pick(locations);
        comp.Coords = spawnLocation.ToMap(EntityManager, _transform);
        Sawmill.Info($"Picked vent location {comp.Coords} for {ToPrettyString(uid):rule}");
    }

    private void OnSelectLocation(Entity<VentSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is { } coords)
            args.Coordinates.Add(coords);
    }

    private void OnSelectEntity(Entity<VentSpawnRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Entity is not { } mob)
            return;

        TryInsertIntoVent(mob);
    }

    private void OnSpawnerUsed(ref GhostRoleSpawnerUsedEvent args)
    {
        if (!HasComp<SpawnInVentSpawnerComponent>(args.Spawner))
            return;

        TryInsertIntoVent(args.Spawned);
    }

    private bool TryInsertIntoVent(EntityUid mob)
    {
        if (!TryComp<VentCrawlerComponent>(mob, out var crawler) || crawler.InTube)
            return false;

        var xform = Transform(mob);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var originalCoords = xform.Coordinates;
        if (!TryFindVentOnTile(grid, gridComp, originalCoords, out var vent, out var entry, out var coords)
            && !TryFindAnyVent(grid, out vent, out entry, out coords))
            return false;

        if (vent == default || entry == null)
            return false;

        if (!coords.Equals(originalCoords))
            _transform.SetCoordinates(mob, coords);

        var holder = Spawn(entry.HolderPrototypeId, coords);
        var holderComp = Comp<VentCrawlerHolderComponent>(holder);

        if (!_container.Insert(mob, holderComp.Container))
        {
            QueueDel(holder);
            return false;
        }

        if (TryComp(mob, out PhysicsComponent? physics))
            _physics.SetCanCollide(mob, false, body: physics);

        if (!_crawable.EnterTube(holder, vent, holderComp))
            return false;

        crawler.InTube = true;
        Dirty(mob, crawler);

        _mover.ResetCamera(mob);
        _mover.SetRelay(mob, holder);
        _eye.RefreshVisibilityMask(mob);
        return true;
    }

    private bool IsVentUsable(EntityUid vent)
    {
        if (!TryComp<VentCrawlerEntryComponent>(vent, out _)
            || !TryComp<VentCrawlerTubeComponent>(vent, out var tube)
            || !tube.Connected)
            return false;

        return !(TryComp<WeldableComponent>(vent, out var weld) && weld.IsWelded);
    }

    private bool TryFindVentOnTile(
        EntityUid grid,
        MapGridComponent gridComp,
        EntityCoordinates coords,
        out EntityUid vent,
        out VentCrawlerEntryComponent? entry,
        out EntityCoordinates ventCoords)
    {
        vent = default;
        entry = null;
        ventCoords = coords;

        var tile = _map.TileIndicesFor(grid, gridComp, coords);
        foreach (var anchored in _map.GetAnchoredEntitiesEnumerator(grid, gridComp, tile))
        {
            if (!IsVentUsable(anchored))
                continue;

            vent = anchored;
            entry = Comp<VentCrawlerEntryComponent>(anchored);
            ventCoords = Transform(anchored).Coordinates;
            return true;
        }

        return false;
    }

    private bool TryFindAnyVent(
        EntityUid grid,
        out EntityUid vent,
        out VentCrawlerEntryComponent? entry,
        out EntityCoordinates ventCoords)
    {
        vent = default;
        entry = null;
        ventCoords = default;

        List<(EntityUid Vent, VentCrawlerEntryComponent Entry, EntityCoordinates Coords)> candidates = [];
        var query = EntityQueryEnumerator<VentCrawlerEntryComponent, VentCrawlerTubeComponent, TransformComponent>();
        while (query.MoveNext(out var candidate, out var candidateEntry, out _, out var transform))
        {
            if (transform.GridUid != grid || !transform.Anchored)
                continue;

            if (!IsVentUsable(candidate))
                continue;

            candidates.Add((candidate, candidateEntry, transform.Coordinates));
        }

        if (candidates.Count == 0)
            return false;

        (vent, entry, ventCoords) = _random.Pick(candidates);
        return true;
    }
}
