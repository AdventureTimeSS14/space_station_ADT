using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.ADT.LogicCircuit.UI;

public static class LogicCircuitStyle
{
    public const string FontPath = "/Fonts/NotoSans/NotoSans-Regular.ttf";
    public const string FontBoldPath = "/Fonts/NotoSans/NotoSans-Bold.ttf";

    public const float NodeWidth = 168f;

    public const float NodeHeaderHeight = 22f;
    public const float NodeRowHeight = 18f;
    public const float NodeFooterHeight = 8f;
    public const float NodeRadius = 5f;
    public const float NodePadding = 8f;

    public const float PinRadius = 4.5f;
    public const float PinGrabRadius = 8f;

    public const float WireWidth = 3f;

    public const float GridStep = 40f;
    public const float MinZoom = 0.35f;
    public const float MaxZoom = 2.5f;

    public static readonly Color Background = Color.FromHex("#0d1114");
    public static readonly Color GridLine = Color.FromHex("#161d22");
    public static readonly Color GridLineMajor = Color.FromHex("#212c34");

    public static readonly Color NodeShadow = Color.FromHex("#00000080");
    public static readonly Color NodeBody = Color.FromHex("#1a2027");
    public static readonly Color NodeBodyDark = Color.FromHex("#141a20");
    public static readonly Color NodeBorder = Color.FromHex("#37424d");
    public static readonly Color NodeBorderSelected = Color.FromHex("#f0cc70");
    public static readonly Color HeaderHighlight = Color.FromHex("#ffffff30");

    public static readonly Color PinWell = Color.FromHex("#0c1013");
    public static readonly Color PinIdle = Color.FromHex("#5d6b78");
    public static readonly Color PinWired = Color.FromHex("#8fa2b3");
    public static readonly Color PinActive = Color.FromHex("#7ad17a");

    public static readonly Color Text = Color.FromHex("#c8d2da");
    public static readonly Color TextDim = Color.FromHex("#7f8b95");
    public static readonly Color TextValue = Color.FromHex("#e0d08a");
    public static readonly Color Error = Color.FromHex("#d07a7a");
    public static readonly Color WirePreview = Color.FromHex("#f0cc70");
    public static readonly Color WireShadow = Color.FromHex("#00000070");
    public static readonly Color SelectionFill = Color.FromHex("#f0cc7020");

    public static readonly Color[] WireColors =
    {
        Color.FromHex("#9fb0bd"),
        Color.FromHex("#d05a5a"),
        Color.FromHex("#5a8ad0"),
        Color.FromHex("#5ad07a"),
        Color.FromHex("#d0a85a"),
        Color.FromHex("#a85ad0"),
    };

    public static Color WireColor(int index)
    {
        if (index < 0 || index >= WireColors.Length)
            return WireColors[0];

        return WireColors[index];
    }

    public static Font CreateFont(IResourceCache cache, int size)
    {
        return new VectorFont(cache.GetResource<FontResource>(FontPath), size);
    }

    public static Font CreateBoldFont(IResourceCache cache, int size)
    {
        return new VectorFont(cache.GetResource<FontResource>(FontBoldPath), size);
    }
}
