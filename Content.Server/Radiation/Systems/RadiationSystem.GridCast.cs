using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Shared.Singularity.Components; // ADT-Tweak
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

// main algorithm that fire radiation rays to target
public partial class RadiationSystem
{
    private readonly record struct SourceData(
        float Intensity,
        float Slope,
        float MaxRange,
        Entity<RadiationSourceComponent, TransformComponent> Entity,
        Vector2 WorldPosition)
    {
<<<<<<< HEAD
        public EntityUid? GridUid => Entity.Comp2.GridUid;
        public float TerminalDecaySlope => Entity.Comp1.TerminalDecaySlope; // ADT-Tweak
        public float TerminalDecayDistance => Entity.Comp1.TerminalDecayDistance; // ADT-Tweak
=======
        public EntityUid Uid => Entity.Owner;
>>>>>>> upstreamwiz/master
        public TransformComponent Transform => Entity.Comp2;
    }

    private void UpdateGridcast()
    {
        var debug = _debugSessions.Count > 0;
        var stopwatch = new Robust.Shared.Timing.Stopwatch();
        stopwatch.Start();

        var sourcesCount = _sourceDataMap.Count;
        if (_activeReceivers.Count == 0 || sourcesCount == 0)
        {
            RaiseLocalEvent(new RadiationSystemUpdatedEvent());
            return;
        }

        var results = new float[_activeReceivers.Count];
        var debugRays = debug ? new ConcurrentBag<DebugRadiationRay>() : null;

        var job = new RadiationJob
        {
            System = this,
            SourceTree = _sourceTree,
            SourceDataMap = _sourceDataMap,
            Destinations = _activeReceivers,
            Results = results,
            DebugRays = debugRays,
            Debug = debug
        };

        _parallel.ProcessNow(job, _activeReceivers.Count);

        for (var i = 0; i < _activeReceivers.Count; i++)
        {
            var uid = _activeReceivers[i];
            var rads = results[i];

            if (_receiverQuery.TryComp(uid, out var receiver))
            {
                receiver.CurrentRadiation = rads;
                if (rads > 0)
                    IrradiateEntity(uid, rads, GridcastUpdateRate);
            }
        }

        if (debugRays is not null)
            UpdateGridcastDebugOverlay(stopwatch.Elapsed.TotalMilliseconds, sourcesCount, _activeReceivers.Count, debugRays.ToList());

        RaiseLocalEvent(new RadiationSystemUpdatedEvent());
    }

    private RadiationRay? Irradiate(SourceData source,
        EntityUid destUid,
        TransformComponent destTrs,
        Vector2 destWorld,
        bool saveVisitedTiles,
        List<Entity<MapGridComponent>> gridList)
    {
        var mapId = destTrs.MapID;
<<<<<<< HEAD

        // get direction from rad source to destination and its distance
        var dir = destWorld - source.WorldPosition;

        // ADT-Tweak start
        var dist = Math.Max(dir.Length(), 0.5f);
        if (TryComp(source.Entity.Owner, out EventHorizonComponent? horizon)) // if we have a horizon emit radiation from the horizon,
            dist = Math.Max(dist - horizon.Radius, 0.5f);
        // Ray enters terminal decay if the distance between source->receiver >TerminalDecayDistance.
        // Decays at an additional linear rate of TerminalDecaySlope rads per tile past TerminalDecayDistance ontop of the existing hyperbolic function.
        // Hyperbolic function
        var rads = source.Intensity / (dist)
        // Terminal decay function
        - (dist - source.TerminalDecayDistance > 0 ? (source.TerminalDecaySlope * (dist - source.TerminalDecayDistance)) : 0);

        if (rads < 0.01)
=======
        var dist = (destWorld - source.WorldPosition).Length();
        var rads = source.Intensity - source.Slope * dist;
        if (rads < MinIntensity)
>>>>>>> upstreamwiz/master
            return null;
        // ADT-Tweak end

        var ray = new RadiationRay(mapId, source.Entity, source.WorldPosition, destUid, destWorld, rads);

        var box = Box2.FromTwoPoints(source.WorldPosition, destWorld);
        gridList.Clear();
        _mapManager.FindGridsIntersecting(mapId, box, ref gridList, true);

        foreach (var grid in gridList)
        {
            ray = Gridcast((grid.Owner, grid.Comp, Transform(grid)), ref ray, saveVisitedTiles, source.Transform, destTrs);
            if (ray.Rads <= 0)
                return ray;
        }

        return ray;
    }

    /// ADT-Tweak start
    /// <summary>
    /// Similar to GridLineEnumerator, but also returns the distance the ray traveled in each cell
    /// </summary>
    /// <param name="sourceGridPos">source of the ray, in grid space</param>
    /// <param name="destGridPos"></param>
    /// <returns></returns>
    private static IEnumerable<(Vector2i cell, float distInCell)> AdvancedGridRaycast(Vector2 sourceGridPos, Vector2 destGridPos)
    {
        var delta = destGridPos - sourceGridPos;

        var currentX = (int)Math.Floor(sourceGridPos.X);
        var currentY = (int)Math.Floor(sourceGridPos.Y);

        var stepX = 0;
        float tDeltaX = 0, tMaxX = float.MaxValue;
        if (delta.X != 0)
        {
            stepX = delta.X > 0 ? 1 : -1;
            float xEdge = stepX > 0 ? currentX + 1 : currentX;
            tMaxX = (xEdge - sourceGridPos.X) / delta.X;
            tDeltaX = stepX / delta.X;
        }

        var stepY = 0;
        float tDeltaY = 0, tMaxY = float.MaxValue;
        if (delta.Y != 0)
        {
            stepY = delta.Y > 0 ? 1 : -1;
            float yEdge = stepY > 0 ? currentY + 1 : currentY;
            tMaxY = (yEdge - sourceGridPos.Y) / delta.Y;
            tDeltaY = stepY / delta.Y;
        }

        var entry = sourceGridPos;
        while (true)
        {
            var tExit = Math.Min(tMaxX, tMaxY);
            var exitIsX = tMaxX < tMaxY;
            if (tExit > 1f)
                tExit = 1f;

            var exit = sourceGridPos + delta * tExit;
            var cell = new Vector2i(currentX, currentY);
            yield return (cell,(exit - entry).Length());
            if (tExit >= 1f)
                break;

            if (exitIsX)
            {
                currentX += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                currentY += stepY;
                tMaxY += tDeltaY;
            }

            entry = exit;
        }
    }
    // ADT-Tweak end

    private RadiationRay Gridcast(
        Entity<MapGridComponent, TransformComponent> grid,
        ref RadiationRay ray,
        bool saveVisitedTiles,
        TransformComponent sourceTrs,
        TransformComponent destTrs)
    {
        var blockers = saveVisitedTiles ? new List<(Vector2i, float)>() : null;
        var gridUid = grid.Owner;
        if (!_resistanceQuery.TryGetComponent(gridUid, out var resistance))
            return ray;

        var resistanceMap = resistance.ResistancePerTile;

<<<<<<< HEAD
        // ADT-Tweak start

        // get coordinate of source and destination in grid coordinates

        // TODO Grid overlap. This currently assumes the grid is always parented directly to the map (local matrix == world matrix).
        // If ever grids are allowed to overlap, this might no longer be true. In that case, this should precompute and cache
        // inverse world matrices.
        var srcLocal = sourceTrs.ParentUid == grid.Owner
=======
        Vector2 srcLocal = sourceTrs.ParentUid == grid.Owner
>>>>>>> upstreamwiz/master
            ? sourceTrs.LocalPosition
            : Vector2.Transform(ray.Source, grid.Comp2.InvLocalMatrix);

        var dstLocal = destTrs.ParentUid == grid.Owner
            ? destTrs.LocalPosition
            : Vector2.Transform(ray.Destination, grid.Comp2.InvLocalMatrix);

        Vector2i sourceGrid = new((int)Math.Floor(srcLocal.X / grid.Comp1.TileSize), (int)Math.Floor(srcLocal.Y / grid.Comp1.TileSize));
        Vector2i destGrid = new((int)Math.Floor(dstLocal.X / grid.Comp1.TileSize), (int)Math.Floor(dstLocal.Y / grid.Comp1.TileSize));

<<<<<<< HEAD
        Vector2i destGrid = new(
            (int)Math.Floor(dstLocal.X / grid.Comp1.TileSize),
            (int)Math.Floor(dstLocal.Y / grid.Comp1.TileSize));

        foreach (var (point,dist) in AdvancedGridRaycast(sourceGrid,destGrid))
        {
            if (resistanceMap.TryGetValue(point, out var resData))
=======
        var line = new GridLineEnumerator(sourceGrid, destGrid);
        while (line.MoveNext())
        {
            var point = line.Current;
            if (!resistanceMap.TryGetValue(point, out var resData))
                continue;

            ray.Rads -= resData;
            if (saveVisitedTiles && blockers is not null)
                blockers.Add((point, ray.Rads));

            if (ray.Rads <= MinIntensity)
>>>>>>> upstreamwiz/master
            {
                var passRatioFromRadResistance = (1 / (resData > 1 ? (resData / 2) : 1));

                var passthroughRatio = MathF.Pow(passRatioFromRadResistance, dist);
                ray.Rads *= passthroughRatio;

                // save data for debug
                if (saveVisitedTiles)
                    blockers!.Add((point, ray.Rads));

                // no intensity left after blocker
                if (ray.Rads <= MinIntensity)
                {
                    ray.Rads = 0;
                    break;
                }
            }
        // ADT-Tweak end
        }

<<<<<<< HEAD

        if (!saveVisitedTiles || blockers!.Count <= 0)
=======
        if (blockers is null || blockers.Count == 0)
>>>>>>> upstreamwiz/master
            return ray;

        ray.Blockers ??= new();
        ray.Blockers.Add(GetNetEntity(gridUid), blockers);
        return ray;
    }

    private float GetAdjustedRadiationIntensity(EntityUid uid, float rads)
    {
        var child = uid;
        var xform = Transform(uid);
        var parent = xform.ParentUid;

        while (parent.IsValid())
        {
            var parentXform = Transform(parent);
            var childMeta = MetaData(child);

            if ((childMeta.Flags & MetaDataFlags.InContainer) != MetaDataFlags.InContainer)
            {
                child = parent;
                parent = parentXform.ParentUid;
                continue;
            }

            if (_blockerQuery.TryComp(xform.ParentUid, out var blocker))
            {
                // ADT-Tweak start
                var ratio =blocker.RadResistance>2? 1 / (blocker.RadResistance/2):1;
                rads *= ratio;
                // ADT-Tweak end
                if (rads < 0)
                    return 0;
            }

            child = parent;
            parent = parentXform.ParentUid;
        }

        return rads;
    }

    [UsedImplicitly]
    private readonly record struct RadiationJob : IParallelRobustJob
    {
        public int BatchSize => 5;
        public required RadiationSystem System { get; init; }
        public required B2DynamicTree<EntityUid> SourceTree { get; init; }
        public required Dictionary<EntityUid, SourceData> SourceDataMap { get; init; }
        public required List<EntityUid> Destinations { get; init; }
        public required float[] Results { get; init; }
        public required ConcurrentBag<DebugRadiationRay>? DebugRays { get; init; }
        public required bool Debug { get; init; }

        public void Execute(int index)
        {
            var destUid = Destinations[index];
            if (System.Deleted(destUid) || !System.TryComp(destUid, out TransformComponent? destTrs))
            {
                Results[index] = 0;
                return;
            }

            var nearbySourcesArray = ArrayPool<EntityUid>.Shared.Rent(256);

            var gridList = new List<Entity<MapGridComponent>>(8);

            try
            {
                var destWorld = System._transform.GetWorldPosition(destTrs);
                var rads = 0f;
                var destMapId = destTrs.MapID;

                var queryAabb = new Box2(destWorld, destWorld);

                var state = (nearbySourcesArray, 0, SourceTree);
                SourceTree.Query(ref state,
                    static (ref (EntityUid[] arr, int count, B2DynamicTree<EntityUid> tree) tuple,
                        DynamicTree.Proxy proxy) =>
                    {
                        if (tuple.count >= tuple.arr.Length)
                            return true;

                        var uid = tuple.tree.GetUserData(proxy);
                        tuple.arr[tuple.count++] = uid;
                        return true;
                    },
                    in queryAabb);

                var nearbySourcesSpan = nearbySourcesArray.AsSpan(0, state.Item2);

                foreach (var sourceUid in nearbySourcesSpan)
                {
                    if (!SourceDataMap.TryGetValue(sourceUid, out var source)
                        || source.Transform.MapID != destMapId)
                        continue;
                    var delta = source.WorldPosition - destWorld;
                    if (delta.LengthSquared() > source.MaxRange * source.MaxRange)
                        continue;
                    var dist = delta.Length();
                    var radsAfterDist = source.Intensity - source.Slope * dist;
                    if (radsAfterDist < System.MinIntensity)
                        continue;
                    if (System.Irradiate(source, destUid, destTrs, destWorld, Debug, gridList) is not { } ray)
                        continue;

                    if (ray.ReachedDestination)
                        rads += ray.Rads;

                    DebugRays?.Add(new DebugRadiationRay(
                        ray.MapId,
                        System.GetNetEntity(ray.SourceUid),
                        ray.Source,
                        System.GetNetEntity(ray.DestinationUid),
                        ray.Destination,
                        ray.Rads,
                        ray.Blockers ?? new Dictionary<NetEntity, List<(Vector2i, float)>>())
                    );
                }

                rads = System.GetAdjustedRadiationIntensity(destUid, rads);
                Results[index] = rads;
            }
            finally
            {
                ArrayPool<EntityUid>.Shared.Return(nearbySourcesArray);
            }
        }
    }
}
