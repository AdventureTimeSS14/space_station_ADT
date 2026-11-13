namespace Content.Client.ADT.Guidebook.Ashwalkers;

public static class AshwalkerGuideColors
{
    public static readonly Color Accent = Color.FromHex("#b0592f");
    public static readonly Color Danger = Color.FromHex("#b0453f");
    public static readonly Color Success = Color.FromHex("#6a9955");
    public static readonly Color Note = Color.FromHex("#8E8E92");
    public static readonly Color ChipBackground = Color.FromHex("#2b2820");
    public static readonly Color BarBackground = Color.FromHex("#2b2820");

    public static Color ForDye(string? dye)
    {
        switch (dye)
        {
            case "Amber":
                return Color.FromHex("#c98a3a");
            case "Cinnabar":
                return Color.FromHex("#9c4a2f");
            case "Crimson":
                return Color.FromHex("#a83232");
            case "Indigo":
                return Color.FromHex("#4b4a9c");
            case "Mint":
                return Color.FromHex("#5fae8c");
            default:
                return Accent;
        }
    }

    public static string ToMarkup(Color color)
    {
        return color.ToHexNoAlpha();
    }
}
