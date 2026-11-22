namespace Content.Client.ADT.Guidebook.WorldAnvil;

public static class WorldAnvilGuideColors
{
    public static readonly Color Accent = Color.FromHex("#d08770");
    public static readonly Color Magmite = Color.FromHex("#ff7a1a");
    public static readonly Color Fuel = Color.FromHex("#bf616a");
    public static readonly Color Inert = Color.FromHex("#7a8290");
    public static readonly Color Card = Color.FromHex("#1c1a22");

    public static string ToMarkup(Color color)
    {
        return color.ToHexNoAlpha();
    }
}