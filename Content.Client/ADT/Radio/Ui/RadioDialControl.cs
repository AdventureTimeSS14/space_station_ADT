using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Radio.Ui;

public sealed class RadioDialControl : Control
{
    private static readonly Color Background = Color.FromHex("#08150F");
    private static readonly Color Border = Color.FromHex("#1E3B2B");
    private static readonly Color Scanline = Color.Black.WithAlpha(0.20f);
    private static readonly Color TickMinor = Color.FromHex("#2C5A41");
    private static readonly Color TickMajor = Color.FromHex("#4EA377");
    private static readonly Color TickLabel = Color.FromHex("#79C79B");
    private static readonly Color Wave = Color.FromHex("#5CE39B");
    private static readonly Color Needle = Color.FromHex("#FF5B4A");
    private static readonly Color NeedleGlow = Color.FromHex("#FF5B4A").WithAlpha(0.14f);

    private const int TickStep = 10;

    private readonly Font _font;

    private float _displayed;
    private float _time;

    public int MinFrequency { get; set; } = 1200;

    public int MaxFrequency { get; set; } = 1600;

    public int Frequency { get; set; } = 1330;

    public bool Transmitting { get; set; }

    public bool Listening { get; set; }

    public RadioDialControl()
    {
        _font = IoCManager.Resolve<IResourceCache>().GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 8);

        MouseFilter = MouseFilterMode.Ignore;
        _displayed = Frequency;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _time += args.DeltaSeconds;

        var delta = Frequency - _displayed;
        if (MathF.Abs(delta) < 0.05f)
        {
            _displayed = Frequency;
            return;
        }

        _displayed += delta * MathF.Min(1f, args.DeltaSeconds * 14f);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var width = PixelWidth;
        var height = PixelHeight;

        if (width <= 0 || height <= 0)
            return;

        var scale = UIScale;
        var span = MathF.Max(1f, MaxFrequency - MinFrequency);

        var baseline = height - _font.GetLineHeight(scale) - 2f * scale;

        handle.DrawRect(new UIBox2(0f, 0f, width, height), Background);

        for (var y = 0; y < height; y += 3)
        {
            handle.DrawRect(new UIBox2(0f, y, width, y + 1f), Scanline);
        }

        DrawWave(handle, width, baseline, scale);
        DrawScale(handle, width, baseline, span, scale);
        DrawNeedle(handle, width, baseline, span, scale);

        handle.DrawRect(new UIBox2(0f, 0f, width, height), Border, false);
    }

    private void DrawWave(DrawingHandleScreen handle, float width, float baseline, float scale)
    {
        var amplitude = Transmitting
            ? 9f * scale
            : Listening
                ? 4f * scale
                : 1.5f * scale;

        var center = baseline * 0.46f;
        var step = MathF.Max(1f, 2f * scale);
        var thickness = MathF.Max(1f, scale);

        for (var x = 0f; x < width; x += step)
        {
            var phase = x / width;
            var offset = MathF.Sin(phase * 26f + _time * 6f) * 0.6f
                         + MathF.Sin(phase * 61f - _time * 9f) * 0.4f;

            var y = center + offset * amplitude;
            handle.DrawRect(new UIBox2(x, y - thickness, MathF.Min(x + step, width), y + thickness), Wave.WithAlpha(0.55f));
        }
    }

    private void DrawScale(DrawingHandleScreen handle, float width, float baseline, float span, float scale)
    {
        handle.DrawRect(new UIBox2(0f, baseline, width, baseline + MathF.Max(1f, scale)), TickMinor);

        for (var frequency = MinFrequency; frequency <= MaxFrequency; frequency += TickStep)
        {
            var x = (frequency - MinFrequency) / span * width;
            var major = frequency % (TickStep * 5) == 0;
            var tickHeight = (major ? 8f : 4f) * scale;
            var halfWidth = MathF.Max(0.5f, scale * 0.5f);

            handle.DrawRect(
                new UIBox2(
                    Math.Clamp(x - halfWidth, 0f, width),
                    baseline - tickHeight,
                    Math.Clamp(x + halfWidth, 0f, width),
                    baseline),
                major ? TickMajor : TickMinor);

            if (!major)
                continue;

            var label = (frequency / 10).ToString();
            var dimensions = handle.GetDimensions(_font, label, scale);
            var labelX = Math.Clamp(x - dimensions.X / 2f, 0f, MathF.Max(0f, width - dimensions.X));

            handle.DrawString(_font, new Vector2(labelX, baseline + 1f * scale), label, scale, TickLabel);
        }
    }

    private void DrawNeedle(DrawingHandleScreen handle, float width, float baseline, float span, float scale)
    {
        var x = (_displayed - MinFrequency) / span * width;
        var halfWidth = MathF.Max(1f, scale);

        DrawBar(handle, x, 7f * scale, width, 0f, baseline, NeedleGlow);
        DrawBar(handle, x, halfWidth, width, 0f, baseline + 3f * scale, Needle);
        DrawBar(handle, x, 4f * scale, width, 0f, 3f * scale, Needle);
    }

    private static void DrawBar(DrawingHandleScreen handle, float center, float halfWidth, float width, float top, float bottom, Color color)
    {
        var left = Math.Clamp(center - halfWidth, 0f, width);
        var right = Math.Clamp(center + halfWidth, 0f, width);

        if (right <= left)
            return;

        handle.DrawRect(new UIBox2(left, top, right, bottom), color);
    }
}
