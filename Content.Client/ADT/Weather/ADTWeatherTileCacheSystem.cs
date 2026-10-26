using Content.Shared.ADT.Weather.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.ADT.Weather;

public sealed class ADTWeatherTileCacheSystem : EntitySystem
{
    private const int ChunkSize = 8;

    [Dependency] private ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedRoofSystem _roof = default!;

    [Dependency] private EntityQuery<BlockWeatherComponent> _blockQuery = default!;
    [Dependency] private EntityQuery<ADTWeatherShelterComponent> _shelterQuery = default!;
    [Dependency] private EntityQuery<RoofComponent> _roofQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private readonly Dictionary<EntityUid, Dictionary<Vector2i, CachedChunk>> _grids = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        SubscribeLocalEvent<BlockWeatherComponent, ComponentStartup>(OnBlockerStartup);
        SubscribeLocalEvent<BlockWeatherComponent, ComponentShutdown>(OnBlockerShutdown);
        SubscribeLocalEvent<BlockWeatherComponent, AnchorStateChangedEvent>(OnBlockerAnchor);
        SubscribeLocalEvent<BlockWeatherComponent, ReAnchorEvent>(OnBlockerReAnchor);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _grids.Clear();
    }

    public bool CanWeatherAffect(Entity<MapGridComponent> grid, TileRef tileRef)
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        if (_shelterQuery.HasComponent(grid))
            return false;

        if (_roofQuery.TryComp(grid, out var roof) && _roof.IsRooved((grid, grid.Comp, roof), tileRef.GridIndices))
            return false;

        var chunk = GetChunk(grid, tileRef.GridIndices, out var bit);
        if ((chunk.Known & bit) != 0)
            return (chunk.Value & bit) != 0;

        var value = IsTileExposed(grid, tileRef);
        chunk.Known |= bit;
        if (value)
            chunk.Value |= bit;
        else
            chunk.Value &= ~bit;

        return value;
    }

    private bool IsTileExposed(Entity<MapGridComponent> grid, TileRef tileRef)
    {
        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];
        if (!tileDef.Weather)
            return false;

        var anchored = _map.GetAnchoredEntities(grid, grid.Comp, tileRef.GridIndices);
        while (anchored.MoveNext(out var uid))
        {
            if (_blockQuery.HasComponent(uid.Value))
                return false;
        }

        return true;
    }

    private CachedChunk GetChunk(EntityUid grid, Vector2i indices, out ulong bit)
    {
        if (!_grids.TryGetValue(grid, out var chunks))
        {
            chunks = new Dictionary<Vector2i, CachedChunk>();
            _grids[grid] = chunks;
        }

        var origin = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        if (!chunks.TryGetValue(origin, out var chunk))
        {
            chunk = new CachedChunk();
            chunks[origin] = chunk;
        }

        bit = GetBit(indices, origin);
        return chunk;
    }

    private static ulong GetBit(Vector2i indices, Vector2i origin)
    {
        var x = indices.X - origin.X * ChunkSize;
        var y = indices.Y - origin.Y * ChunkSize;
        return 1UL << (y * ChunkSize + x);
    }

    private void Invalidate(EntityUid grid, Vector2i indices)
    {
        if (!_grids.TryGetValue(grid, out var chunks))
            return;

        var origin = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        if (!chunks.TryGetValue(origin, out var chunk))
            return;

        chunk.Known &= ~GetBit(indices, origin);
    }

    private void Invalidate(TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return;

        Invalidate(gridUid, _map.CoordinatesToTile(gridUid, grid, xform.Coordinates));
    }

    private void OnTileChanged(ref TileChangedEvent ev)
    {
        if (!_grids.ContainsKey(ev.Entity))
            return;

        foreach (var change in ev.Changes)
        {
            Invalidate(ev.Entity, change.GridIndices);
        }
    }

    private void OnGridRemoval(GridRemovalEvent ev)
    {
        _grids.Remove(ev.EntityUid);
    }

    private void OnBlockerStartup(Entity<BlockWeatherComponent> ent, ref ComponentStartup args)
    {
        Invalidate(Transform(ent));
    }

    private void OnBlockerShutdown(Entity<BlockWeatherComponent> ent, ref ComponentShutdown args)
    {
        Invalidate(Transform(ent));
    }

    private void OnBlockerAnchor(Entity<BlockWeatherComponent> ent, ref AnchorStateChangedEvent args)
    {
        Invalidate(args.Transform);
    }

    private void OnBlockerReAnchor(Entity<BlockWeatherComponent> ent, ref ReAnchorEvent args)
    {
        Invalidate(args.OldGrid, args.TilePos);
        Invalidate(args.Grid, args.TilePos);
    }

    private sealed class CachedChunk
    {
        public ulong Known;
        public ulong Value;
    }
}
