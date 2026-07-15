using Robust.Client.Graphics;
using Robust.Client.UserInterface;

// File lives under ADT/, but keeps the Medical.CrewMonitoring namespace so XAML
// can resolve it via xmlns:ui without a separate clr-namespace (XamlIL + IDE).
namespace Content.Client.Medical.CrewMonitoring;

/// <summary>
/// CRT-style alternating horizontal scanlines (one pixel light, one dark).
/// </summary>
public sealed class CrtScanlineBackground : Control
{
    public Color LightLine { get; set; } = Color.FromHex("#2E2E34");
    public Color DarkLine { get; set; } = Color.FromHex("#1A1A1E");

    public CrtScanlineBackground()
    {
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        for (var y = 0; y < PixelHeight; y++)
        {
            var color = (y & 1) == 0 ? LightLine : DarkLine;
            handle.DrawRect(new UIBox2(0, y, PixelWidth, y + 1), color);
        }
    }
}
