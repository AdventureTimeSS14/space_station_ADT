using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TTS.Sanitize;

[DataDefinition]
public sealed partial class TTSSanitizeCountedUnit : ITTSSanitizeRule
{
    /// <summary>
    /// How the unit is written after the number
    /// </summary>
    [DataField(required: true)]
    public string Unit = string.Empty;

    /// <summary>
    /// Form for 1, 21, 101 and so on
    /// </summary>
    [DataField(required: true)]
    public string One = string.Empty;

    /// <summary>
    /// Form for 2 to 4, 22 to 24 and so on
    /// </summary>
    [DataField(required: true)]
    public string Few = string.Empty;

    /// <summary>
    /// Form for everything else, including 5 to 20 and the teens
    /// </summary>
    [DataField(required: true)]
    public string Many = string.Empty;

    private Regex? _regex;

    private Regex Compiled => _regex ??= new Regex(
        $@"(\d+)\s*{Regex.Escape(Unit)}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Apply(string text)
    {
        return Compiled.Replace(text, Replace);
    }

    private string Replace(Match match)
    {
        if (!long.TryParse(match.Groups[1].Value, out var number))
            return match.Value;

        return $"{match.Groups[1].Value} {Pick(number)}";
    }

    private string Pick(long number)
    {
        number = Math.Abs(number);

        if (number % 100 >= 11 && number % 100 <= 14)
            return Many;

        return (number % 10) switch
        {
            1 => One,
            2 or 3 or 4 => Few,
            _ => Many,
        };
    }
}
