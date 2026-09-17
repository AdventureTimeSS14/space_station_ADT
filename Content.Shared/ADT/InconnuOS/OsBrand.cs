namespace Content.Shared.ADT.InconnuOS;

public static class OsBrand
{
    public const string Name = "InconnuOS";

    public const string Publisher = "Schrodinger Entertainment";

    public const string Version = "4.04";

    public const string Build = "17763.SE";

    public const string Codename = "Superposition";

    public static string FullVersion => $"{Name} {Version} [сборка {Build}]";

    public static string Copyright => $"(c) {Publisher}. Все права защищены.";
}
