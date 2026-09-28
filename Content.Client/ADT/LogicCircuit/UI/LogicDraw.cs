using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client.ADT.LogicCircuit.UI;

public static class LogicDraw
{
    private const int CornerSegments = 6;

    private static readonly Vector2[] CornerBuffer = new Vector2[2 + 4 * (CornerSegments + 1)];

    public static void RoundedRect(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        radius = Math.Clamp(radius, 0f, MathF.Min(box.Width, box.Height) / 2f);

        if (radius <= 0.5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        var count = 0;

        CornerBuffer[count++] = box.Center;

        AddCorner(ref count, new Vector2(box.Right - radius, box.Top + radius), radius, -90f);
        AddCorner(ref count, new Vector2(box.Right - radius, box.Bottom - radius), radius, 0f);
        AddCorner(ref count, new Vector2(box.Left + radius, box.Bottom - radius), radius, 90f);
        AddCorner(ref count, new Vector2(box.Left + radius, box.Top + radius), radius, 180f);

        CornerBuffer[count++] = CornerBuffer[1];

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, CornerBuffer.AsSpan(0, count), color);
    }

    public static void RoundedTop(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        radius = Math.Clamp(radius, 0f, MathF.Min(box.Width / 2f, box.Height));

        if (radius <= 0.5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        var count = 0;

        CornerBuffer[count++] = box.Center;

        AddCorner(ref count, new Vector2(box.Right - radius, box.Top + radius), radius, -90f);
        CornerBuffer[count++] = new Vector2(box.Right, box.Bottom);
        CornerBuffer[count++] = new Vector2(box.Left, box.Bottom);
        AddCorner(ref count, new Vector2(box.Left + radius, box.Top + radius), radius, 180f);

        CornerBuffer[count++] = CornerBuffer[1];

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, CornerBuffer.AsSpan(0, count), color);
    }

    private static void AddCorner(ref int count, Vector2 center, float radius, float startDegrees)
    {
        for (var i = 0; i <= CornerSegments; i++)
        {
            var angle = (startDegrees + 90f * i / CornerSegments) * MathF.PI / 180f;

            CornerBuffer[count++] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
    }

    public const int VerticesPerSegment = 6;
    public static void ThickPolyline(
        DrawingHandleScreen handle,
        ReadOnlySpan<Vector2> points,
        float width,
        Color color,
        Vector2[] buffer)
    {
        if (points.Length < 2)
            return;

        var half = MathF.Max(0.5f, width / 2f);
        var written = 0;

        for (var i = 0; i < points.Length - 1; i++)
        {
            if (written + VerticesPerSegment > buffer.Length)
                break;

            var from = points[i];
            var to = points[i + 1];
            var delta = to - from;
            var length = delta.Length();

            if (length <= 0.0001f)
                continue;

            var direction = delta / length;
            var normal = new Vector2(-direction.Y, direction.X) * half;
            var overlap = direction * (half * 0.5f);

            var start = from - overlap;
            var end = to + overlap;

            buffer[written++] = start + normal;
            buffer[written++] = start - normal;
            buffer[written++] = end - normal;
            buffer[written++] = start + normal;
            buffer[written++] = end - normal;
            buffer[written++] = end + normal;
        }

        if (written == 0)
            return;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, buffer.AsSpan(0, written), color);
    }

    public static void TextInBand(
        DrawingHandleScreen handle,
        Font font,
        Vector2 topLeft,
        float bandHeight,
        float maxWidth,
        string text,
        float scale,
        Color color,
        bool alignRight = false)
    {
        var clipped = Clip(handle, font, text, scale, maxWidth);
        var size = handle.GetDimensions(font, clipped, scale);

        var x = alignRight ? topLeft.X + maxWidth - size.X : topLeft.X;
        var y = topLeft.Y + (bandHeight - size.Y) / 2f;

        handle.DrawString(font, new Vector2(x, y), clipped, scale, color);
    }

    public static string Clip(DrawingHandleScreen handle, Font font, string text, float scale, float maxWidth)
    {
        if (maxWidth <= 0f || text.Length == 0)
            return string.Empty;

        if (handle.GetDimensions(font, text, scale).X <= maxWidth)
            return text;

        for (var length = text.Length - 1; length > 0; length--)
        {
            var candidate = string.Concat(text.AsSpan(0, length), "...");

            if (handle.GetDimensions(font, candidate, scale).X <= maxWidth)
                return candidate;
        }

        return string.Empty;
    }
}
