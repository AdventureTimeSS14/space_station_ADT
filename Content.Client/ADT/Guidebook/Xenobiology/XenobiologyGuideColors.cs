namespace Content.Client.ADT.Guidebook.Xenobiology;

public static class XenobiologyGuideColors
{
    public static readonly Color Card = Color.FromHex("#1c1a22");
    public static readonly Color Border = Color.FromHex("#4a4458");
    public static readonly Color Muted = Color.FromHex("#7a8290");
    public static readonly Color Accent = Color.FromHex("#a3be8c");
    public static readonly Color Arrow = Color.FromHex("#858094");
    public static readonly Color NodeOutline = Color.FromHex("#141318");

    public static readonly Color Water = Color.FromHex("#5c9bd6");
    public static readonly Color Plasma = Color.FromHex("#ff9f1a");
    public static readonly Color Blood = Color.FromHex("#bf616a");
    public static readonly Color Radium = Color.FromHex("#4dff4d");

    public static Color ForReagent(string reagentId)
    {
        return reagentId switch
        {
            "Water" => Water,
            "Plasma" => Plasma,
            "Blood" => Blood,
            "Radium" => Radium,
            _ => Muted,
        };
    }

    public static Color ForTier(int tier)
    {
        return tier switch
        {
            0 => Color.FromHex("#7a8290"),
            1 => Color.FromHex("#d08770"),
            2 => Color.FromHex("#b48ead"),
            3 => Color.FromHex("#88c0d0"),
            _ => Color.FromHex("#a3be8c"),
        };
    }

    public static string ToMarkup(Color color)
    {
        return color.ToHexNoAlpha();
    }
}