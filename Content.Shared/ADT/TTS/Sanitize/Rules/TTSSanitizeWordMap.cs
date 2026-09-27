using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TTS.Sanitize;

[DataDefinition]
public sealed partial class TTSSanitizeWordMap : ITTSSanitizeRule
{
    [DataField]
    public string Pattern = @"(?<![a-zA-Zа-яёА-ЯЁ0-9])[a-zA-Zа-яёА-ЯЁ0-9]+(?![a-zA-Zа-яёА-ЯЁ0-9])";

    [DataField(required: true)]
    public Dictionary<string, string> Words = new();

    private Regex? _regex;

    private Regex Compiled => _regex ??= new Regex(
        Pattern,
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public string Apply(string text)
    {
        return Compiled.Replace(text, Replace);
    }

    private string Replace(Match match)
    {
        return Words.TryGetValue(match.Value.ToLowerInvariant(), out var replacement)
            ? replacement
            : match.Value;
    }
}
