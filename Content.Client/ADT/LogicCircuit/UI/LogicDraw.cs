using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client.ADT.LogicCircuit.UI;

public static class LogicDraw
{
    public static void RoundedRect(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        var maxRadius = MathF.Min(box.Width, box.Height) / 2f;
        radius = Math.Clamp(radius, 0f, maxRadius);

        if (radius <= 0.5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        handle.DrawRect(new UIBox2(box.Left + radius, box.Top, box.Right - radius, box.Bottom), color);
        handle.DrawRect(new UIBox2(box.Left, box.Top + radius, box.Left + radius, box.Bottom - radius), color);
        handle.DrawRect(new UIBox2(box.Right - radius, box.Top + radius, box.Right, box.Bottom - radius), color);

        handle.DrawCircle(new Vector2(box.Left + radius, box.Top + radius), radius, color);
        handle.DrawCircle(new Vector2(box.Right - radius, box.Top + radius), radius, color);
        handle.DrawCircle(new Vector2(box.Left + radius, box.Bottom - radius), radius, color);
        handle.DrawCircle(new Vector2(box.Right - radius, box.Bottom - radius), radius, color);
    }

    public static void RoundedTop(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        radius = Math.Clamp(radius, 0f, MathF.Min(box.Width / 2f, box.Height));

        if (radius <= 0.5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        handle.DrawRect(new UIBox2(box.Left + radius, box.Top, box.Right - radius, box.Bottom), color);
        handle.DrawRect(new UIBox2(box.Left, box.Top + radius, box.Right, box.Bottom), color);

        handle.DrawCircle(new Vector2(box.Left + radius, box.Top + radius), radius, color);
        handle.DrawCircle(new Vector2(box.Right - radius, box.Top + radius), radius, color);
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
