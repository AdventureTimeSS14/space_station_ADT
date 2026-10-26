namespace Content.Client.ADT.Guidebook.Heretic;

public static class HereticGuideColors
{
    public static readonly Color Accent = Color.FromHex("#8b6fa8");
    public static readonly Color Ascension = Color.FromHex("#ebcb8b");
    public static readonly Color Side = Color.FromHex("#7a8290");
    public static readonly Color Ingredient = Color.FromHex("#3b3547");
    public static readonly Color Output = Color.FromHex("#39473b");

    public static Color ForPath(string? path)
    {
        switch (path)
        {
            case "Ash":
                return Color.FromHex("#d08770");
            case "Blade":
                return Color.FromHex("#c3cad6");
            case "Flesh":
                return Color.FromHex("#bf616a");
            case "Void":
                return Color.FromHex("#88c0d0");
            case "Rust":
                return Color.FromHex("#a3844e");
            case "Cosmos":
                return Color.FromHex("#b48ead");
            default:
                return Accent;
        }
    }

    public static string ToMarkup(Color color)
    {
        return color.ToHexNoAlpha();
    }
}
