using System.Text.RegularExpressions;

namespace Content.Shared.ADT.Chat;

public static class SharedADTD20Emote
{
    public const int Faces = 20;
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan RollDuration = TimeSpan.FromSeconds(1.5);
    public const string RollSoundCollection = "Dice";

    private const string MarkerOpen = "\u227A";
    private const string MarkerClose = "\u227B";

    private static readonly Regex MarkerRegex = new(@"\s*\u227Ad20:(\d{1,2})\u227B[.!?…]*", RegexOptions.Compiled);

    public static string MakeMarker(int roll)
    {
        return $"{MarkerOpen}d20:{roll}{MarkerClose}";
    }

    public static bool TryGetRoll(string message, out int roll)
    {
        roll = 0;

        if (string.IsNullOrEmpty(message))
            return false;

        var match = MarkerRegex.Match(message);
        if (!match.Success)
            return false;

        if (!int.TryParse(match.Groups[1].Value, out roll))
            return false;

        return roll >= 1 && roll <= Faces;
    }

    public static string ReplaceMarker(string message, string replacement)
    {
        return MarkerRegex.Replace(message, _ => replacement);
    }

    public static string GetTierLocId(int roll)
    {
        if (roll <= 1)
            return "adt-d20-emote-tier-critical-failure";

        if (roll <= 9)
            return "adt-d20-emote-tier-failure";

        if (roll <= 14)
            return "adt-d20-emote-tier-partial";

        if (roll <= 19)
            return "adt-d20-emote-tier-success";

        return "adt-d20-emote-tier-critical-success";
    }

    public static string GetTierColor(int roll)
    {
        if (roll <= 1)
            return "#e04b4b";

        if (roll <= 9)
            return "#e0913c";

        if (roll <= 14)
            return "#d8c92b";

        if (roll <= 19)
            return "#5ec14e";

        return "#43e06f";
    }
}
