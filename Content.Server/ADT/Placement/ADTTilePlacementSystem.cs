using System.Numerics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network.Messages;

namespace Content.Server.ADT.Placement;

public sealed class ADTTilePlacementSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly Vector2i[] Neighbours =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    private List<Entity<MapGridComponent>> _grids = new();

    public void SnapTileToAdjacentGrid(MsgPlacement msg)
    {
        if (msg.PlaceType != PlacementManagerMessage.RequestPlacement || !msg.IsTile || msg.TileType == 0)
            return;

        var coordinates = GetCoordinates(msg.NetCoordinates);
        if (!coordinates.IsValid(EntityManager) || HasComp<MapGridComponent>(coordinates.EntityId))
            return;

        var mapCoordinates = _transform.ToMapCoordinates(coordinates);
        if (mapCoordinates.MapId == MapId.Nullspace || _map.TryFindGridAt(mapCoordinates, out _, out _))
            return;

        _grids.Clear();
        var box = Box2.CenteredAround(mapCoordinates.Position, new Vector2(3f, 3f));
        _map.FindGridsIntersecting(mapCoordinates.MapId, box, ref _grids);

        foreach (var (gridUid, grid) in _grids)
        {
            var local = _transform.ToCoordinates(gridUid, mapCoordinates);
            var tile = _map.LocalToTile(gridUid, grid, local);

            if (!HasAdjacentTile(gridUid, grid, tile))
                continue;

            msg.NetCoordinates = GetNetCoordinates(_map.GridTileToLocal(gridUid, grid, tile));
            return;
        }
    }

    private bool HasAdjacentTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var offset in Neighbours)
        {
            if (_map.TryGetTileRef(gridUid, grid, tile + offset, out var tileRef) && !tileRef.Tile.IsEmpty)
                return true;
        }

        return false;
    }
}
