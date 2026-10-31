using System.Numerics;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class CanvasSelection
{
    public readonly int Left;
    public readonly int Top;
    public readonly int Width;
    public readonly int Height;
    public readonly Vector2[] Polygon;

    private readonly bool[] _mask;

    private CanvasSelection(int left, int top, int width, int height, Vector2[] polygon, bool[] mask)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        Polygon = polygon;
        _mask = mask;
    }

    public int Right => Left + Width;

    public int Bottom => Top + Height;

    public bool Contains(int x, int y)
    {
        x -= Left;
        y -= Top;
        return x >= 0 && y >= 0 && x < Width && y < Height && _mask[y * Width + x];
    }

    public static CanvasSelection FromMask(int left, int top, int width, int height, bool[] mask)
    {
        var polygon = new Vector2[4];
        polygon[0] = new Vector2(left, top);
        polygon[1] = new Vector2(left + width, top);
        polygon[2] = new Vector2(left + width, top + height);
        polygon[3] = new Vector2(left, top + height);
        return new CanvasSelection(left, top, width, height, polygon, mask);
    }

    public static CanvasSelection? FromRect(int left, int top, int right, int bottom, int canvasWidth, int canvasHeight)
    {
        var points = new List<Vector2>(4)
        {
            new(left, top),
            new(right, top),
            new(right, bottom),
            new(left, bottom),
        };
        return FromPolygon(points, canvasWidth, canvasHeight);
    }

    public static CanvasSelection? FromPolygon(List<Vector2> points, int canvasWidth, int canvasHeight)
    {
        if (points.Count < 3)
            return null;

        var polygon = points.ToArray();
        var min = polygon[0];
        var max = polygon[0];
        foreach (var point in polygon)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        var left = Math.Clamp((int) MathF.Floor(min.X), 0, canvasWidth);
        var top = Math.Clamp((int) MathF.Floor(min.Y), 0, canvasHeight);
        var right = Math.Clamp((int) MathF.Ceiling(max.X), 0, canvasWidth);
        var bottom = Math.Clamp((int) MathF.Ceiling(max.Y), 0, canvasHeight);
        if (right <= left || bottom <= top)
            return null;

        var width = right - left;
        var height = bottom - top;
        var rows = new List<float>?[height];

        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            if (a.Y == b.Y)
                continue;

            var upper = a.Y < b.Y ? a : b;
            var lower = a.Y < b.Y ? b : a;
            var slope = (lower.X - upper.X) / (lower.Y - upper.Y);
            var first = Math.Max(top, (int) MathF.Ceiling(upper.Y - 0.5f));
            var last = Math.Min(bottom - 1, (int) MathF.Ceiling(lower.Y - 0.5f) - 1);

            for (var y = first; y <= last; y++)
            {
                var row = rows[y - top] ??= new List<float>();
                row.Add(upper.X + (y + 0.5f - upper.Y) * slope);
            }
        }

        var mask = new bool[width * height];
        var any = false;

        for (var y = 0; y < height; y++)
        {
            if (rows[y] is not { } crossings)
                continue;

            crossings.Sort();
            for (var i = 0; i + 1 < crossings.Count; i += 2)
            {
                var from = Math.Max(left, (int) MathF.Ceiling(crossings[i] - 0.5f));
                var to = Math.Min(right, (int) MathF.Ceiling(crossings[i + 1] - 0.5f));
                for (var x = from; x < to; x++)
                {
                    mask[y * width + x - left] = true;
                    any = true;
                }
            }
        }

        return any ? new CanvasSelection(left, top, width, height, polygon, mask) : null;
    }

    public CanvasSelection? Transformed(Vector2 start, Vector2 end, int canvasWidth, int canvasHeight)
    {
        var origin = new Vector2(Left, Top);
        var scale = (end - start) / new Vector2(Width, Height);
        var points = new List<Vector2>(Polygon.Length);
        foreach (var point in Polygon)
        {
            points.Add(start + (point - origin) * scale);
        }

        return FromPolygon(points, canvasWidth, canvasHeight);
    }
}
