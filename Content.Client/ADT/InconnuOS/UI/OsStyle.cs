using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.ADT.InconnuOS.UI;

public static class OsStyle
{
    public const string FontPath = "/Fonts/NotoSans/NotoSans-Regular.ttf";
    public const string FontBoldPath = "/Fonts/NotoSans/NotoSans-Bold.ttf";
    public const string FontMonoPath = "/Fonts/RobotoMono/RobotoMono-Regular.ttf";

    public const float TitleBarHeight = 30f;
    public const float TaskbarHeight = 38f;
    public const float WindowRadius = 4f;
    public const float ResizeGrip = 14f;
    public const float CaptionButtonWidth = 44f;

    public const float DesktopIconWidth = 78f;
    public const float DesktopIconHeight = 72f;
    public const float DesktopIconSize = 34f;
    public const float DesktopPadding = 12f;

    public const float MinWindowWidth = 260f;
    public const float MinWindowHeight = 140f;

    public static readonly Color DesktopTop = Color.FromHex("#101a2a");
    public static readonly Color DesktopBottom = Color.FromHex("#060a12");

    public static readonly Color WindowBody = Color.FromHex("#1c1f24");
    public static readonly Color WindowHeader = Color.FromHex("#23272d");
    public static readonly Color WindowHeaderIdle = Color.FromHex("#1b1e22");
    public static readonly Color WindowBorder = Color.FromHex("#363a41");
    public static readonly Color Shadow = Color.FromHex("#00000070");

    public static readonly Color Taskbar = Color.FromHex("#16181c");
    public static readonly Color TaskbarLine = Color.FromHex("#2c3036");
    public static readonly Color TaskbarButton = Color.FromHex("#16181c");
    public static readonly Color TaskbarButtonHover = Color.FromHex("#2b2f35");

    public static readonly Color Panel = Color.FromHex("#191b1f");
    public static readonly Color PanelLight = Color.FromHex("#25282d");

    public static readonly Color Text = Color.FromHex("#ccd4dd");
    public static readonly Color TextDim = Color.FromHex("#7c8896");
    public static readonly Color TextBright = Color.FromHex("#eaf0f6");

    public static readonly Color Error = Color.FromHex("#d07a7a");
    public static readonly Color Good = Color.FromHex("#7ad17a");

    public static readonly Color CrashBackground = Color.FromHex("#1b3a6b");
    public static readonly Color BootBackground = Color.FromHex("#05070b");

    public static readonly Color Watermark = Color.FromHex("#ffffff28");

    public static Font CreateFont(IResourceCache cache, int size)
    {
        return new VectorFont(cache.GetResource<FontResource>(FontPath), size);
    }

    public static Font CreateBoldFont(IResourceCache cache, int size)
    {
        return new VectorFont(cache.GetResource<FontResource>(FontBoldPath), size);
    }

    public static Font CreateMonoFont(IResourceCache cache, int size)
    {
        return new VectorFont(cache.GetResource<FontResource>(FontMonoPath), size);
    }

    public static Color Mix(Color from, Color to, float amount)
    {
        return Color.InterpolateBetween(from, to, Math.Clamp(amount, 0f, 1f));
    }

    public static Color AccentSoft(Color accent)
    {
        return Mix(WindowBody, accent, 0.35f);
    }
}
