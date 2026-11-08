using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server.ADT.Salvage.Systems;

public static class ADTMegafaunaGeometry
{
    public static readonly Direction[] Cardinals =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    public static readonly Direction[] Diagonals =
    {
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    public static readonly Direction[] AllDirs =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    public static IEnumerable<MapCoordinates> RangeTiles(MapCoordinates center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                yield return new MapCoordinates(center.Position + new Vector2(x, y), center.MapId);
            }
        }
    }

    public static IEnumerable<MapCoordinates> RingTiles(MapCoordinates center, int radius)
    {
        if (radius == 0)
        {
            yield return center;
            yield break;
        }

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (Math.Abs(x) != radius && Math.Abs(y) != radius)
                    continue;

                yield return new MapCoordinates(center.Position + new Vector2(x, y), center.MapId);
            }
        }
    }

    public static MapCoordinates Offset(MapCoordinates coords, Direction dir, int distance)
    {
        return new MapCoordinates(coords.Position + dir.ToVec() * distance, coords.MapId);
    }

    public static float ChebyshevDistance(MapCoordinates a, MapCoordinates b)
    {
        if (a.MapId != b.MapId)
            return float.MaxValue;

        var delta = a.Position - b.Position;
        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
    }

    public static Vector2 ByondAngleToVector(float degrees)
    {
        var radians = MathHelper.DegreesToRadians(degrees);
        return new Vector2(MathF.Sin(radians), MathF.Cos(radians));
    }

    public static float VectorToByondAngle(Vector2 vector)
    {
        var degrees = MathHelper.RadiansToDegrees(MathF.Atan2(vector.X, vector.Y));

        if (degrees < 0f)
            degrees += 360f;

        return degrees;
    }

    public static List<Vector2i> GetLine(Vector2i from, Vector2i to)
    {
        var result = new List<Vector2i>();

        var x = from.X;
        var y = from.Y;
        var dx = Math.Abs(to.X - from.X);
        var dy = -Math.Abs(to.Y - from.Y);
        var sx = from.X < to.X ? 1 : -1;
        var sy = from.Y < to.Y ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            result.Add(new Vector2i(x, y));

            if (x == to.X && y == to.Y)
                break;

            var e2 = 2 * err;

            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
        }

        return result;
    }
}
