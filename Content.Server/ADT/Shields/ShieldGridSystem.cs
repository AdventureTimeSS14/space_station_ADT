using System.Collections.Generic;
using System.Numerics;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Shields;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server.ADT.Shields;

/// <summary>Считает, где будет поле (hull/bubble), и спавнит/удаляет сегменты по тайлам.</summary>
public sealed partial class ShieldGridSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly ShieldSegmentSystem _segment = default!;
    [Dependency] private readonly ShieldDiffusionSystem _diffusion = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly Dictionary<(EntityUid Grid, Vector2i Tile), EntityUid> _segmentsByTile = new();

    /// <summary>Кэш hull-тайлов по гриду.</summary>
    private readonly Dictionary<EntityUid, HashSet<Vector2i>> _hullCache = new();

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

    public override void Initialize()
    {
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        SubscribeLocalEvent<ShieldSegmentRemovedEvent>(OnSegmentRemoved);
        SubscribeLocalEvent<ShieldGeneratorComponent, ComponentShutdown>(OnGeneratorShutdown);
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

        _hullCache[gridUid] = result;
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

        if (_segmentsByTile.TryGetValue((gridUid, idx), out var existing))
        {
            if (Exists(existing) && gen.Segments.Contains(existing))
                return;

            _segmentsByTile.Remove((gridUid, idx));
            if (Exists(existing))
                QueueDel(existing);
        }

        var coords = new EntityCoordinates(gridUid, (idx + new Vector2(0.5f, 0.5f)) * grid.TileSize);
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

            foreach (var change in ev.Changes)
            {
                var changed = change.GridIndices;
                foreach (var cand in GetAffectedCandidates(changed))
                {
                    var needs = IsSpace(gridUid, gridComp, cand) && HasNonSpaceNeighbor(gridUid, gridComp, cand);
                    var existing = GetSegmentAt(gridUid, cand);

                    if (needs && !Exists(existing))
                    {
                        SpawnSegment(genUid, gen, gridUid, gridComp, cand);
                    }
                    else if (!needs && Exists(existing))
                    {
                        if (gen.Segments.Remove(existing))
                        {
                            gen.DamagedTiles.Remove(cand);
                            gen.Tiles.Remove(cand);
                            _segmentsByTile.Remove((gridUid, cand));
                            QueueDel(existing);
                        }
                    }
                }
            }
        }
    }

    private void UpdateHullCacheForTile(EntityUid gridUid, MapGridComponent grid, HashSet<Vector2i> hull, Vector2i changed)
    {
        foreach (var cand in GetAffectedCandidates(changed))
        {
            var isHull = IsSpace(gridUid, grid, cand) && HasNonSpaceNeighbor(gridUid, grid, cand);
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