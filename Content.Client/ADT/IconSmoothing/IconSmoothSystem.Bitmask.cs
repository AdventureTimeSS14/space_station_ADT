using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
    private const int BitmaskNorth = 1;
    private const int BitmaskSouth = 2;
    private const int BitmaskEast = 4;
    private const int BitmaskWest = 8;
    private const int BitmaskNorthEast = 16;
    private const int BitmaskSouthEast = 32;
    private const int BitmaskSouthWest = 64;
    private const int BitmaskNorthWest = 128;

    private void CalculateNewSpriteBitmask(Entity<MapGridComponent>? gridEntity, IconSmoothComponent smooth, Entity<SpriteComponent> sprite, TransformComponent xform)
    {
        if (gridEntity == null)
        {
            _sprite.LayerSetRsiState(sprite.AsNullable(), 0, $"{smooth.StateBase}0");
            return;
        }

        var gridUid = gridEntity.Value.Owner;
        var grid = gridEntity.Value.Comp;
        var pos = _mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);

        var north = HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.North);
        var south = HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.South);
        var east = HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.East);
        var west = HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.West);

        var mask = 0;
        if (north)
            mask |= BitmaskNorth;
        if (south)
            mask |= BitmaskSouth;
        if (east)
            mask |= BitmaskEast;
        if (west)
            mask |= BitmaskWest;

        if (north && east && HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.NorthEast))
            mask |= BitmaskNorthEast;
        if (south && east && HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.SouthEast))
            mask |= BitmaskSouthEast;
        if (south && west && HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.SouthWest))
            mask |= BitmaskSouthWest;
        if (north && west && HasBitmaskNeighbour(smooth, gridUid, grid, pos, Direction.NorthWest))
            mask |= BitmaskNorthWest;

        _sprite.LayerSetRsiState(sprite.AsNullable(), 0, $"{smooth.StateBase}{mask}");
    }

    private bool HasBitmaskNeighbour(IconSmoothComponent smooth, EntityUid gridUid, MapGridComponent grid, Vector2i pos, Direction direction)
    {
        return MatchingEntity(smooth, _mapSystem.GetAnchoredEntities(gridUid, grid, pos.Offset(direction)));
    }
}
