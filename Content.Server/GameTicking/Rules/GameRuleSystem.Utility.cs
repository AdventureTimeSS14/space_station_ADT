using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
<<<<<<< ours
using Robust.Shared.Random;
using Robust.Shared.Utility;
||||||| base
=======
using Robust.Shared.Utility;
>>>>>>> theirs

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    [Dependency] private readonly StationSystem _station = default!; // ADT

    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent>();
    }

    /// <summary>
    /// Queries all gamerules, regardless of if they're active or not.
    /// </summary>
    protected EntityQueryEnumerator<T, GameRuleComponent> QueryAllRules()
    {
        return EntityQueryEnumerator<T, GameRuleComponent>();
    }

    /// <summary>
    ///     Utility function for finding a random event-eligible station entity
    /// </summary>
    protected bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[RobustRandom.Next(stations.Count)];
        return true;
    }

    protected bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    // ADT-Start
    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords,
        int numAttempts = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

<<<<<<< ours
        if (GetStationMainGrid(station.Comp) is not { } grid)
            return false;
||||||| base
        // Weight grid choice by tilecount
        var weights = new Dictionary<Entity<MapGridComponent>, float>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var comp))
                continue;
=======
        // Weight grid choice by tilecount
        var totalTiles = 0;
        var grids = new List<(Entity<MapGridComponent> Entity, int Count, List<TileRef> Tiles)>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var comp))
                continue;
>>>>>>> theirs

<<<<<<< ours
        targetGrid = grid.Owner;
        return TryFindTileOnGrid(grid, out tile, out targetCoords);
    }
||||||| base
            weights.Add((possibleTarget, comp), _map.GetAllTiles(possibleTarget, comp).Count());
        }
=======
            // Get the tile count for the given grid.
            var tileCount = _map.GetFilledTileCount((possibleTarget, comp));

            // Just to be sure, no empty elements.
            if (tileCount > 0)
            {
                grids.Add(((possibleTarget, comp), tileCount, new()));
                totalTiles += tileCount;
            }
        }
>>>>>>> theirs

<<<<<<< ours
    protected Entity<MapGridComponent>? GetStationMainGrid(StationDataComponent station)
    {
        if ((station.Grids.FirstOrNull(HasComp<BecomesStationComponent>) ?? _station.GetLargestGrid(station.Owner)) is not
            { } grid || !TryComp(grid, out MapGridComponent? gridComp))
            return null;

        return (grid, gridComp);
    }
||||||| base
        if (weights.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }
=======
        if (grids.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }
>>>>>>> theirs

<<<<<<< ours
    protected bool TryFindTileOnGrid(Entity<MapGridComponent> grid,
        out Vector2i tile,
        out EntityCoordinates targetCoords,
        int tries = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;

        var aabb = grid.Comp.LocalAABB;

        for (var i = 0; i < tries; i++)
||||||| base
        (targetGrid, var gridComp) = RobustRandom.Pick(weights);

        var found = false;
        var aabb = gridComp.LocalAABB;

        for (var i = 0; i < 10; i++)
=======
        for (var i = 0; i < numAttempts; i++)
>>>>>>> theirs
        {
            // Find random tile within list.
            var nextTileIndex = RobustRandom.Next(totalTiles);
            TileRef? randomTileRef = null;
            MapGridComponent gridComp = default!;
            var startIndex = 0;
            for (int j = 0; j < grids.Count; j++)
            {
                var grid = grids[j];
                // If the index is in this particular grid, find it and remove the tile to prevent selecting it twice.
                if (nextTileIndex >= startIndex + grid.Count)
                {
                    startIndex += grid.Count;
                    continue;
                }

                (targetGrid, gridComp) = grid.Entity;

                // Empty list: hasn't been queried yet - get our tiles.
                if (grid.Tiles.Count <= 0)
                {
                    grid.Tiles = _map.GetAllTiles(targetGrid, gridComp).ToList();

                    // Actual list count doesn't match expected count (a bug - return failure).
                    Debug.Assert(grid.Tiles.Count == grid.Count);
                    if (grid.Tiles.Count != grid.Count)
                        return false;
                }

                var ourTileIndex = nextTileIndex - startIndex;
                randomTileRef = grid.Tiles[ourTileIndex];
                grid.Tiles.RemoveSwap(ourTileIndex);
                grid.Count--;
                totalTiles--;

                // Empty list, remove element
                if (grid.Tiles.Count <= 0)
                    grids.RemoveSwap(j);

                break;
            }

<<<<<<< ours
            tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(grid.Owner, Transform(grid.Owner).MapUid, tile)
                || _atmosphere.IsTileAirBlocked(grid.Owner, tile, mapGridComp: grid.Comp))
||||||| base
            tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile)
                || _atmosphere.IsTileAirBlockedCached(targetGrid, tile))
            {
=======
            // Out of valid tiles, return early.
            if (randomTileRef is not { } tileRef)
                return false;

            // Invalid tile, try again.
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tileRef.GridIndices)
                || _atmosphere.IsTileAirBlockedCached(targetGrid, tileRef.GridIndices))
            {
>>>>>>> theirs
                continue;

<<<<<<< ours
            targetCoords = _map.GridTileToLocal(grid.Owner, grid.Comp, tile);
            return true;
||||||| base
            found = true;
            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);
            break;
=======
            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tileRef.GridIndices);
            tile = tileRef.GridIndices;
            return true;
>>>>>>> theirs
        }

        return false;
    }
    // ADT-End

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }
}
