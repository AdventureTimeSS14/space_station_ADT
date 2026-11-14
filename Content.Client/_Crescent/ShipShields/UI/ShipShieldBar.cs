using System.Numerics;
using Content.Shared._Crescent.ShipShields;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._Crescent.ShipShields.UI;

/// <summary>
/// Horizontal HP bar for the ship shield emitter. Shows the shield charge percentage
/// while online, and a real-time countdown to recharge while offline.
/// </summary>
public sealed class ShipShieldBar : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Font _font;

    private ShipShieldState _state;

    public ShipShieldBar()
    {
        IoCManager.InjectDependencies(this);
        _font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
        MinSize = new Vector2(100, 18);
    }

    public void SetState(ShipShieldState state)
    {
        _state = state;
        Visible = state.HasShield;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_state.HasShield)
            return;

        var background = Color.FromHex("#1a1a1a");
        var border = Color.FromHex("#444444");

        handle.DrawRect(new UIBox2(0, 0, PixelWidth, PixelHeight), background);

        var percent = MathHelper.Clamp(_state.Percent, 0f, 1f);

        Color barColor;
        string label;
        if (_state.Online)
        {
            barColor = LerpHpColor(percent);
            label = $"{(int) MathF.Round(percent * 100f)}%";
        }
        else
        {
            barColor = Color.FromHex("#a02020");
            var remaining = _state.RechargeEndTime.HasValue
                ? (_state.RechargeEndTime.Value - _timing.CurTime).TotalSeconds
                : 0;
            if (remaining < 0)
                remaining = 0;
            label = FormatCountdown(remaining);
        }

        var fillWidth = PixelWidth * percent;
        if (fillWidth > 1)
            handle.DrawRect(new UIBox2(0, 0, fillWidth, PixelHeight), barColor);

        handle.DrawRect(new UIBox2(0, 0, PixelWidth, PixelHeight), border, filled: false);

        var dimensions = handle.GetDimensions(_font, label, UIScale);
        var textPos = new Vector2((PixelWidth - dimensions.X) * 0.5f, (PixelHeight - dimensions.Y) * 0.5f);
        handle.DrawString(_font, textPos, label, UIScale, Color.White);
    }

    private static Color LerpHpColor(float percent)
    {
        if (percent >= 0.5f)
        {
            var t = (percent - 0.5f) * 2f;
            return Color.InterpolateBetween(Color.FromHex("#d8a000"), Color.FromHex("#1ea35a"), t);
        }
        else
        {
            var t = percent * 2f;
            return Color.InterpolateBetween(Color.FromHex("#a02020"), Color.FromHex("#d8a000"), t);
        }
    }

    private static string FormatCountdown(double seconds)
    {
        if (seconds <= 0)
            return "0.0s";
        if (seconds < 60)
            return $"{seconds:0.0}s";
        var mins = (int) (seconds / 60);
        var secs = seconds - mins * 60;
        return $"{mins}m {secs:00}s";
    }
}
