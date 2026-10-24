using System.Collections.Generic;
using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Shields;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;

namespace Content.Server.ADT.Shields;

/// <summary>Считает, где будет поле (hull/bubble), и спавнит/удаляет сегменты по тайлам.</summary>
public sealed partial class ShieldGridSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly ShieldSegmentSystem _segment = default!;
    [Dependency] private readonly ShieldDiffusionSystem _diffusion = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), EntityUid> _segmentsByTile = new();

    /// <summary>Кэш hull-тайлов по гриду.</summary>
    private readonly Dictionary<EntityUid, HashSet<Vector2i>> _hullCache = new();

    /// <summary>Тайлы корпуса, исключённые из поля из-за стыковочных шлюзов (перед ними должен быть проход).</summary>
    private readonly Dictionary<EntityUid, HashSet<Vector2i>> _dockExclusionsByGrid = new();

    /// <summary>Генераторы по гриду, чтобы OnTileChanged не перебирал все генераторы раунда.</summary>
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _generatorsByGrid = new();

    private readonly record struct SegmentSpawnRequest(
        EntityUid GenUid,
        ShieldGeneratorComponent Gen,
        EntityUid GridUid,
        MapGridComponent Grid,
        Vector2i Tile);

    private readonly Dictionary<EntityUid, Queue<SegmentSpawnRequest>> _pendingSpawns = new();
    private readonly Dictionary<EntityUid, Queue<EntityUid>> _pendingDeletes = new();

    public static readonly Vector2i[] CardinalVectors =
    {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
    };

    private const float CoverageProbeSize = 0.6f;

    private static readonly Vector2 TileCenterOffset = new(0.5f, 0.5f);

    public override void Initialize()
    {
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<ShieldSegmentRemovedEvent>(OnSegmentRemoved);
        SubscribeLocalEvent<ShieldGeneratorComponent, ComponentShutdown>(OnGeneratorShutdown);
        SubscribeLocalEvent<PhysicsComponent, MoveEvent>(OnGridMoved);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        ProcessPendingDeletes();
        ProcessPendingSpawns();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _segmentsByTile.Clear();
        _hullCache.Clear();
        _dockExclusionsByGrid.Clear();
        _generatorsByGrid.Clear();
        _pendingSpawns.Clear();
        _pendingDeletes.Clear();
        ShieldAirtightRegistry.Clear();
    }

    private void OnSegmentRemoved(ref ShieldSegmentRemovedEvent ev)
    {
        _segmentsByTile.Remove((ev.Grid, ev.Tile));
    }

    private void OnGeneratorShutdown(EntityUid uid, ShieldGeneratorComponent gen, ComponentShutdown args)
    {
        UntrackGenerator(uid);
        RemoveAllSegments(uid, gen);
    }

    public void TrackGenerator(EntityUid uid)
    {
        var gridUid = Transform(uid).GridUid;
        if (gridUid is not { } grid)
            return;

        if (!_generatorsByGrid.TryGetValue(grid, out var set))
        {
            set = new HashSet<EntityUid>();
            _generatorsByGrid[grid] = set;
        }

        set.Add(uid);
    }

    public void UntrackGenerator(EntityUid uid)
    {
        foreach (var set in _generatorsByGrid.Values)
            set.Remove(uid);
    }

    public bool IsSpace(EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        var tileRef = _map.GetTileRef(gridUid, grid, indices);
        return tileRef.Tile.IsEmpty || _turf.IsSpace(tileRef);
    }

    private bool HasNonSpaceNeighbor(EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        foreach (var dir in CardinalVectors)
        {
            if (!IsSpace(gridUid, grid, indices + dir))
                return true;
        }
        return false;
    }

    public EntityUid GetSegmentAt(EntityUid gridUid, Vector2i tile)
        => _segmentsByTile.GetValueOrDefault((gridUid, tile));

    private HashSet<Vector2i> GetHullTiles(EntityUid gridUid, MapGridComponent grid)
    {
        if (_hullCache.TryGetValue(gridUid, out var cached))
            return cached;

        var result = new HashSet<Vector2i>();
        foreach (var tileRef in _map.GetAllTiles(gridUid, grid))
        {
            var idx = tileRef.GridIndices;
            if (IsSpace(gridUid, grid, idx))
                continue;

            foreach (var dir in CardinalVectors)
            {
                var neighbor = idx + dir;
                if (IsSpace(gridUid, grid, neighbor))
                    result.Add(neighbor);
            }
        }

        var exclusions = GetDockExclusions(gridUid, grid);
        _dockExclusionsByGrid[gridUid] = exclusions;
        result.ExceptWith(exclusions);

        _hullCache[gridUid] = result;
        return result;
    }

    /// <summary>
    ///     Тайл перед стыковочным шлюзом поле не должно перекрывать место
    /// </summary>
    private HashSet<Vector2i> GetDockExclusions(EntityUid gridUid, MapGridComponent grid)
    {
        var result = new HashSet<Vector2i>();
        var query = EntityQueryEnumerator<DockingComponent>();
        while (query.MoveNext(out var dockUid, out _))
        {
            var dockXform = Transform(dockUid);
            if (dockXform.GridUid != gridUid)
                continue;

            var dockTile = _map.LocalToTile(gridUid, grid, dockXform.Coordinates);
            var frontVec = dockXform.LocalRotation.ToWorldVec();
            var frontTile = dockTile + new Vector2i((int) MathF.Round(frontVec.X), (int) MathF.Round(frontVec.Y));
            if (IsSpace(gridUid, grid, frontTile))
                result.Add(frontTile);
        }

        return result;
    }

    public void GenerateField(EntityUid genUid, ShieldGeneratorComponent gen)
    {
        RemoveAllSegments(genUid, gen);

        if (gen.Running != ShieldRunningState.Running)
            return;

        if (Transform(genUid).GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComp))
            return;

        var tiles = GetHullTiles(gridUid, gridComp);

        var queue = new Queue<SegmentSpawnRequest>(tiles.Count);
        _pendingSpawns[genUid] = queue;
        foreach (var idx in tiles)
            queue.Enqueue(new SegmentSpawnRequest(genUid, gen, gridUid, gridComp, idx));
    }

    private void ProcessPendingSpawns()
    {
        if (_pendingSpawns.Count == 0)
            return;

        var done = new List<EntityUid>();
        foreach (var (genUid, queue) in _pendingSpawns)
        {
            if (TerminatingOrDeleted(genUid))
            {
                done.Add(genUid);
                continue;
            }

            var perTick = _cfg.GetCVar(SimpleStationCCVars.ShieldSegmentsPerTick);
            for (var i = 0; i < perTick && queue.Count > 0; i++)
            {
                var request = queue.Dequeue();
                SpawnSegment(request.GenUid, request.Gen, request.GridUid, request.Grid, request.Tile);
            }

            if (queue.Count == 0)
                done.Add(genUid);
        }

        foreach (var genUid in done)
            _pendingSpawns.Remove(genUid);
    }

    private void ProcessPendingDeletes()
    {
        if (_pendingDeletes.Count == 0)
            return;

        var done = new List<EntityUid>();
        foreach (var (genUid, queue) in _pendingDeletes)
        {
            var perTick = _cfg.GetCVar(SimpleStationCCVars.ShieldSegmentsPerTick);
            for (var i = 0; i < perTick && queue.Count > 0; i++)
            {
                var segment = queue.Dequeue();
                if (!Exists(segment))
                    continue;

                if (TryComp<ShieldSegmentComponent>(segment, out var segComp)
                    && Transform(segment).GridUid is { } segGrid)
                {
                    _segmentsByTile.Remove((segGrid, segComp.GridPos));
                }

                QueueDel(segment);
            }

            if (queue.Count == 0)
                done.Add(genUid);
        }

        foreach (var genUid in done)
            _pendingDeletes.Remove(genUid);
    }

    private void SpawnSegment(EntityUid genUid, ShieldGeneratorComponent gen, EntityUid gridUid, MapGridComponent grid, Vector2i idx)
    {
        if (_diffusion.IsDiffused(gridUid, idx))
            return;

        var coords = _map.GridTileToLocal(gridUid, grid, idx);
        if (IsCoveredByForeignGrid(gridUid, coords))
            return;

        if (_segmentsByTile.TryGetValue((gridUid, idx), out var existing))
        {
            if (Exists(existing))
            {
                if (gen.Segments.Contains(existing))
                    return;

                if (TryComp<ShieldSegmentComponent>(existing, out var existingSeg)
                    && existingSeg.Generator is { } owner
                    && owner != genUid
                    && TryComp<ShieldGeneratorComponent>(owner, out var ownerGen)
                    && ownerGen.Running == ShieldRunningState.Running)
                    return;

                QueueDel(existing);
            }

            _segmentsByTile.Remove((gridUid, idx));
        }

        var segment = Spawn(gen.SegmentProto, coords);
        var segComp = Comp<ShieldSegmentComponent>(segment);
        segComp.Generator = genUid;
        segComp.Grid = gridUid;
        segComp.GridPos = idx;
        gen.Segments.Add(segment);
        gen.Tiles[idx] = new ShieldTileData();
        _segmentsByTile[(gridUid, idx)] = segment;
        _segment.UpdateSegmentVisuals(segment, segComp, gen);
    }

    private List<Entity<MapGridComponent>> _gridQueryBuffer = new();

    private bool IsCoveredByForeignGrid(EntityUid gridUid, EntityCoordinates coords)
    {
        var xform = Transform(gridUid);
        if (xform.MapUid == null)
            return false;

        var worldPos = _transform.ToWorldPosition(coords);
        var box = Box2.CenteredAround(worldPos, new Vector2(CoverageProbeSize, CoverageProbeSize));
        _gridQueryBuffer.Clear();
        _mapManager.FindGridsIntersecting(xform.MapID, box, ref _gridQueryBuffer);
        foreach (var grid in _gridQueryBuffer)
        {
            if (grid.Owner != gridUid)
                return true;
        }

        return false;
    }

    private void EnsureSegment(EntityUid genUid, ShieldGeneratorComponent gen, EntityUid gridUid, MapGridComponent grid, Vector2i tile, bool shouldExist)
    {
        var existing = GetSegmentAt(gridUid, tile);

        if (shouldExist)
        {
            if (!Exists(existing))
                SpawnSegment(genUid, gen, gridUid, grid, tile);
            return;
        }

        if (!Exists(existing))
            return;

        if (TryComp<ShieldSegmentComponent>(existing, out var segComp) && segComp.Generator != genUid)
            return;

        gen.Segments.Remove(existing);
        gen.DamagedTiles.Remove(tile);
        gen.Tiles.Remove(tile);
        _segmentsByTile.Remove((gridUid, tile));
        QueueDel(existing);
    }

    private void OnGridMoved(EntityUid uid, PhysicsComponent phys, ref MoveEvent args)
    {
        if (_generatorsByGrid.Count == 0)
            return;

        // MoveEvent подписан на PhysicsComponent, поэтому фильтруем до гридов.
        if (!TryComp<MapGridComponent>(uid, out var movedGrid))
            return;

        var movedXform = Transform(uid);
        if (movedXform.MapUid == null)
            return;

        var movedWorldMatrix = _transform.GetWorldMatrix(uid);
        var movedBounds = movedWorldMatrix.TransformBox(movedGrid.LocalAABB).Enlarged(movedGrid.TileSize * 2f);

        foreach (var (genGridUid, generators) in _generatorsByGrid)
        {
            if (genGridUid == uid)
                continue;

            if (!_hullCache.TryGetValue(genGridUid, out var hull))
                continue;

            if (!TryComp<MapGridComponent>(genGridUid, out var genGrid))
                continue;

            var genXform = Transform(genGridUid);
            if (genXform.MapUid == null || genXform.MapID != movedXform.MapID)
                continue;

            var genWorldMatrix = _transform.GetWorldMatrix(genGridUid);
            var genBounds = genWorldMatrix.TransformBox(genGrid.LocalAABB);
            if (!genBounds.Intersects(movedBounds))
                continue;

            var tileScale = genGrid.TileSize;
            var minTile = _map.WorldToTile(genGridUid, genGrid, movedBounds.BottomLeft);
            var maxTile = _map.WorldToTile(genGridUid, genGrid, movedBounds.TopRight);

            for (var x = minTile.X; x <= maxTile.X; x++)
            {
                for (var y = minTile.Y; y <= maxTile.Y; y++)
                {
                    var tile = new Vector2i(x, y);
                    if (!hull.Contains(tile))
                        continue;

                    var worldPos = Vector2.Transform((tile + TileCenterOffset) * tileScale, genWorldMatrix);
                    var covered = movedBounds.Contains(worldPos);

                    foreach (var genUid in generators)
                    {
                        if (!TryComp<ShieldGeneratorComponent>(genUid, out var gen))
                            continue;

                        if (gen.Running != ShieldRunningState.Running)
                            continue;

                        EnsureSegment(genUid, gen, genGridUid, genGrid, tile, !covered);
                    }
                }
            }
        }
    }

    public void RemoveAllSegments(EntityUid genUid, ShieldGeneratorComponent gen)
    {
        _pendingSpawns.Remove(genUid);

        if (!_pendingDeletes.TryGetValue(genUid, out var queue))
        {
            queue = new Queue<EntityUid>();
            _pendingDeletes[genUid] = queue;
        }

        foreach (var segment in gen.Segments)
        {
            if (Exists(segment))
                queue.Enqueue(segment);
        }

        gen.Segments.Clear();
        gen.Tiles.Clear();
        gen.DamagedTiles.Clear();
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        if (!TryComp<MapGridComponent>(ev.Entity, out var gridComp))
            return;

        var gridUid = ev.Entity;

        if (_hullCache.TryGetValue(gridUid, out var hull))
        {
            foreach (var change in ev.Changes)
                UpdateHullCacheForTile(gridUid, gridComp, hull, change.GridIndices);
        }

        if (!_generatorsByGrid.TryGetValue(gridUid, out var generators))
            return;

        foreach (var genUid in generators)
        {
            if (!TryComp<ShieldGeneratorComponent>(genUid, out var gen))
                continue;

            if (gen.Running != ShieldRunningState.Running)
                continue;

            var exclusions = _dockExclusionsByGrid.TryGetValue(gridUid, out var excl) ? excl : null;
            foreach (var change in ev.Changes)
            {
                var changed = change.GridIndices;
                foreach (var cand in GetAffectedCandidates(changed))
                {
                    var needs = IsSpace(gridUid, gridComp, cand) && HasNonSpaceNeighbor(gridUid, gridComp, cand);
                    if (needs && exclusions != null && exclusions.Contains(cand))
                        needs = false;

                    EnsureSegment(genUid, gen, gridUid, gridComp, cand, needs);
                }
            }
        }
    }

    private void UpdateHullCacheForTile(EntityUid gridUid, MapGridComponent grid, HashSet<Vector2i> hull, Vector2i changed)
    {
        var exclusions = _dockExclusionsByGrid.TryGetValue(gridUid, out var excl) ? excl : null;
        foreach (var cand in GetAffectedCandidates(changed))
        {
            var isHull = IsSpace(gridUid, grid, cand) && HasNonSpaceNeighbor(gridUid, grid, cand);
            if (isHull && exclusions != null && exclusions.Contains(cand))
                isHull = false;

            if (isHull)
                hull.Add(cand);
            else
                hull.Remove(cand);
        }
    }

    private static IEnumerable<Vector2i> GetAffectedCandidates(Vector2i changed)
    {
        yield return changed;
        foreach (var dir in CardinalVectors)
            yield return changed + dir;
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        _hullCache.Remove(ev.EntityUid);
        _dockExclusionsByGrid.Remove(ev.EntityUid);
        _generatorsByGrid.Remove(ev.EntityUid);

        if (_segmentsByTile.Count == 0)
            return;

        List<(EntityUid Grid, Vector2i Tile)> toRemove = new();
        foreach (var key in _segmentsByTile.Keys)
        {
            if (key.Grid == ev.EntityUid)
                toRemove.Add(key);
        }

        foreach (var key in toRemove)
            _segmentsByTile.Remove(key);
    }
}