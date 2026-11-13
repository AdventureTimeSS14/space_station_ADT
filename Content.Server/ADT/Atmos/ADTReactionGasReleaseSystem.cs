using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.ADT.Atmos;

public sealed class ADTReactionGasReleaseSystem : EntitySystem
{
    private const int MaxSearchDepth = 3;

    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly Queue<(Vector2i Tile, int Depth)> _queue = new();
    private readonly HashSet<Vector2i> _visited = new();

    public GasMixture? GetReleaseMixture(Entity<TransformComponent> ent)
    {
        var mixture = _atmosphere.GetContainingMixture(ent.AsNullable(), false, true);

        if (ent.Comp.GridUid is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid)
            || !TryComp<GridAtmosphereComponent>(gridUid, out var gridAtmos))
        {
            return mixture;
        }

        var origin = _transform.GetGridTilePositionOrDefault(ent.AsNullable());
        if (mixture != _atmosphere.GetTileMixture(gridUid, ent.Comp.MapUid, origin))
            return mixture;

        var gridEnt = new Entity<MapGridComponent, GridAtmosphereComponent>(gridUid, grid, gridAtmos);
        if (IsOpen(gridEnt, origin))
            return mixture;

        if (FindOpenTile(gridEnt, origin) is not { } target)
            return mixture;

        return _atmosphere.GetTileMixture(gridUid, ent.Comp.MapUid, target, true) ?? mixture;
    }

    private Vector2i? FindOpenTile(Entity<MapGridComponent, GridAtmosphereComponent> grid, Vector2i origin)
    {
        _queue.Clear();
        _visited.Clear();
        _queue.Enqueue((origin, 0));
        _visited.Add(origin);

        while (_queue.TryDequeue(out var current))
        {
            if (current.Depth >= MaxSearchDepth)
                continue;

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var dir = (AtmosDirection) (1 << i);
                var next = current.Tile.Offset(dir);

                if (!_visited.Add(next))
                    continue;

                if (!CanPass(grid, current.Tile, next, dir))
                    continue;

                if (IsOpen(grid, next))
                    return next;

                _queue.Enqueue((next, current.Depth + 1));
            }
        }

        return null;
    }

    private bool CanPass(Entity<MapGridComponent, GridAtmosphereComponent> grid, Vector2i from, Vector2i to, AtmosDirection dir)
    {
        if (IsFanTile(grid, from) || IsFanTile(grid, to))
            return true;

        return IsConnected(grid, from, to, dir);
    }

    private bool IsOpen(Entity<MapGridComponent, GridAtmosphereComponent> grid, Vector2i tile)
    {
        if (IsFanTile(grid, tile))
            return false;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var dir = (AtmosDirection) (1 << i);
            var neighbor = tile.Offset(dir);

            if (IsFanTile(grid, neighbor))
                continue;

            if (IsConnected(grid, tile, neighbor, dir))
                return true;
        }

        return false;
    }

    private bool IsConnected(Entity<MapGridComponent, GridAtmosphereComponent> grid, Vector2i from, Vector2i to, AtmosDirection dir)
    {
        var atmos = new Entity<GridAtmosphereComponent?>(grid.Owner, grid.Comp2);

        return !_atmosphere.IsTileAirBlockedCached(atmos, from, dir)
            && !_atmosphere.IsTileAirBlockedCached(atmos, to, dir.GetOpposite());
    }

    private bool IsFanTile(Entity<MapGridComponent, GridAtmosphereComponent> grid, Vector2i tile)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(grid.Owner, grid.Comp1, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<ADTGasPassableComponent>(uid))
                return true;
        }

        return false;
    }
}
