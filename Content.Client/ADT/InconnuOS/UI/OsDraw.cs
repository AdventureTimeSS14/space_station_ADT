using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client.ADT.InconnuOS.UI;

public static class OsDraw
{
    private const int CornerSegments = 6;

    private static readonly Vector2[] Buffer = new Vector2[2 + 4 * (CornerSegments + 1)];

    public static void RoundedRect(DrawingHandleScreen handle, UIBox2 box, float radius, Color color)
    {
        radius = Math.Clamp(radius, 0f, MathF.Min(box.Width, box.Height) / 2f);

        if (radius <= 0.5f)
        {
            handle.DrawRect(box, color);
            return;
        }

        var count = 0;

        Buffer[count++] = box.Center;

        AddCorner(ref count, new Vector2(box.Right - radius, box.Top + radius), radius, -90f);
        AddCorner(ref count, new Vector2(box.Right - radius, box.Bottom - radius), radius, 0f);
        AddCorner(ref count, new Vector2(box.Left + radius, box.Bottom - radius), radius, 90f);
        AddCorner(ref count, new Vector2(box.Left + radius, box.Top + radius), radius, 180f);

        Buffer[count++] = Buffer[1];

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Buffer.AsSpan(0, count), color);
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

        Buffer[count++] = box.Center;

        AddCorner(ref count, new Vector2(box.Right - radius, box.Top + radius), radius, -90f);
        Buffer[count++] = new Vector2(box.Right, box.Bottom);
        Buffer[count++] = new Vector2(box.Left, box.Bottom);
        AddCorner(ref count, new Vector2(box.Left + radius, box.Top + radius), radius, 180f);

        Buffer[count++] = Buffer[1];

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Buffer.AsSpan(0, count), color);
    }

    private static void AddCorner(ref int count, Vector2 center, float radius, float startDegrees)
    {
        for (var i = 0; i <= CornerSegments; i++)
        {
            var angle = (startDegrees + 90f * i / CornerSegments) * MathF.PI / 180f;

            Buffer[count++] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
    }

    public static void Framed(DrawingHandleScreen handle, UIBox2 box, float radius, float border, Color frame, Color fill)
    {
        RoundedRect(handle, box, radius, frame);
        RoundedRect(handle, Shrink(box, border), MathF.Max(0f, radius - border), fill);
    }

    public static void Shadow(DrawingHandleScreen handle, UIBox2 box, float radius, float spread, float strength)
    {
        const int layers = 6;

        if (strength <= 0f)
            return;

        var drop = spread * 0.35f;

        for (var i = layers; i >= 1; i--)
        {
            var grow = spread * i / layers;
            var layer = new UIBox2(box.Left - grow, box.Top - grow + drop, box.Right + grow, box.Bottom + grow + drop);

            RoundedRect(handle, layer, radius + grow, Color.Black.WithAlpha(strength / layers));
        }
    }

    public static UIBox2 Shrink(UIBox2 box, float amount)
    {
        return new UIBox2(box.Left + amount, box.Top + amount, box.Right - amount, box.Bottom - amount);
    }

    public static UIBox2 Lerp(UIBox2 from, UIBox2 to, float t)
    {
        return new UIBox2(
            MathHelper.Lerp(from.Left, to.Left, t),
            MathHelper.Lerp(from.Top, to.Top, t),
            MathHelper.Lerp(from.Right, to.Right, t),
            MathHelper.Lerp(from.Bottom, to.Bottom, t));
    }

    public static UIBox2 ScaleAround(UIBox2 box, float scale)
    {
        var center = box.Center;
        var half = box.Size * scale / 2f;

        return new UIBox2(center - half, center + half);
    }

    public static float EaseOutCubic(float t)
    {
        var inverse = 1f - Math.Clamp(t, 0f, 1f);

        return 1f - inverse * inverse * inverse;
    }

    public static float EaseInCubic(float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        return t * t * t;
    }

    public static float EaseInOutSine(float t)
    {
        return -(MathF.Cos(MathF.PI * Math.Clamp(t, 0f, 1f)) - 1f) / 2f;
    }

    public static void Spinner(DrawingHandleScreen handle, Vector2 center, float radius, float dot, float time, Color color)
    {
        const int dots = 5;
        const float cycle = 2.6f;
        const float delay = 0.11f;
        const float visible = 0.86f;

        for (var i = 0; i < dots; i++)
        {
            var phase = ((time - i * delay) % cycle + cycle) % cycle / cycle;

            if (phase > visible)
                continue;

            var progress = EaseInOutSine(phase / visible);
            var angle = -MathF.PI / 2f + progress * MathF.PI * 4f;

            handle.DrawCircle(center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius, dot, color);
        }
    }
}
