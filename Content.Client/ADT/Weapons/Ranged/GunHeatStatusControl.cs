using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Atmos;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Weapons.Ranged;

public sealed class GunHeatStatusControl : Control
{
    private static readonly Color _background = Color.Black.WithAlpha(0.45f);
    private static readonly Color _cool = Color.FromHex("#e0b050");
    private static readonly Color _hot = Color.FromHex("#ff3b1f");
    private static readonly Color _jamMarker = Color.FromHex("#ffd34d");
    private static readonly Color _meltMarker = Color.FromHex("#ff4d4d");

    private float _fill;
    private float _glow;

    public GunHeatStatusControl()
    {
        MinHeight = 7;
        HorizontalExpand = true;
        Margin = new Thickness(0, 2, 0, 0);
    }

    public void Update(GunHeatComponent heat)
        => (_fill, _glow) = GetFractions(heat);

    /// <summary>
    /// Fill is the temperature across the gauge. The marker is where the barrel starts to glow.
    /// </summary>
    public static (float Fill, float Glow) GetFractions(GunHeatComponent heat)
    {
        var ceiling = heat.FailureTemperature > Atmospherics.T20C ? heat.FailureTemperature : heat.MeltTemperature;
        var range = ceiling - Atmospherics.T20C;
        if (range <= 0f)
            return (0f, 1f);

        return (Math.Clamp((heat.Temperature - Atmospherics.T20C) / range, 0f, 1f),
            Math.Clamp((heat.GlowTemperature - Atmospherics.T20C) / range, 0f, 1f));
    }

    /// <summary>
    /// Draws the heat gauge on the screen.
    /// </summary>
    public static void DrawGauge(DrawingHandleScreen handle, UIBox2 box, float fill, float glow, float marker)
    {
        handle.DrawRect(box, _background);

        if (fill > 0f)
        {
            var color = fill < glow ? Color.InterpolateBetween(_cool, _hot, fill / MathF.Max(glow, 0.01f) * 0.5f) : _hot;
            handle.DrawRect(new UIBox2(box.Left + 1, box.Top + 1, box.Left + 1 + ((box.Width - 2) * fill), box.Bottom - 1), color);
        }

        var glowX = box.Left + ((box.Width - 2) * glow);
        handle.DrawRect(new UIBox2(glowX, box.Top, glowX + marker, box.Bottom), _jamMarker);
        handle.DrawRect(new UIBox2(box.Right - marker, box.Top, box.Right, box.Bottom), _meltMarker);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        DrawGauge(handle, new UIBox2(0, 0, PixelWidth, PixelHeight), _fill, _glow, MathF.Max(1f, UIScale));
    }
}
