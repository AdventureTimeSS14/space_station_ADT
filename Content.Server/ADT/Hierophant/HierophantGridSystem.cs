using System.Numerics;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Map;

namespace Content.Server.ADT.Hierophant;

public sealed class HierophantGridSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public EntityCoordinates Anchor(MapCoordinates coords)
    {
        if (coords.MapId == MapId.Nullspace)
            return EntityCoordinates.Invalid;

        if (_mapMan.TryFindGridAt(coords, out var gridUid, out var grid))
            return _transform.ToCoordinates(gridUid, coords).SnapToGrid(grid);

        return _transform.ToCoordinates(coords).SnapToGrid(EntityManager);
    }

    public EntityCoordinates Anchor(EntityUid uid)
    {
        return Anchor(_transform.GetMapCoordinates(uid));
    }

    public EntityCoordinates Offset(EntityCoordinates origin, Direction dir, int distance)
    {
        return origin.Offset(dir.ToVec() * distance);
    }

    public IEnumerable<EntityCoordinates> RangeTiles(EntityCoordinates center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                yield return center.Offset(new Vector2(x, y));
            }
        }
    }

    /// <summary>
    /// Chebyshev distance in tiles, measured in the frame of <paramref name="a"/>.
    /// </summary>
    public float TileDistance(EntityCoordinates a, EntityCoordinates b)
    {
        if (!a.IsValid(EntityManager) || !b.IsValid(EntityManager))
            return float.MaxValue;

        if (_transform.GetMapId(a) != _transform.GetMapId(b))
            return float.MaxValue;

        var delta = _transform.WithEntityId(b, a.EntityId).Position - a.Position;
        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
    }
}
