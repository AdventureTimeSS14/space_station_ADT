using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TTS.Sanitize;

[DataDefinition]
public sealed partial class TTSSanitizeNumbers : ITTSSanitizeRule
{
    [DataField]
    public int MaxDigits = 9;

    private Regex? _regex;

    private Regex Compiled => _regex ??= new Regex(@"\d+", RegexOptions.Compiled);

    public string Apply(string text)
    {
        return Compiled.Replace(text, Replace);
    }

    private string Replace(Match match)
    {
        if (match.Value.Length > MaxDigits || !long.TryParse(match.Value, out var number))
            return match.Value;

        return NumberConverter.NumberToText(number);
    }
}
