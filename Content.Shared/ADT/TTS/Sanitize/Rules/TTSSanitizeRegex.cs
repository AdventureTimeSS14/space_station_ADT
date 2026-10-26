using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TTS.Sanitize;

[DataDefinition]
public sealed partial class TTSSanitizeRegex : ITTSSanitizeRule
{
    [DataField(required: true)]
    public string Pattern = string.Empty;

    [DataField]
    public string Replacement = string.Empty;

    [DataField]
    public bool IgnoreCase = true;

    private Regex? _regex;

    private Regex Compiled => _regex ??= new Regex(
        Pattern,
        RegexOptions.Compiled | RegexOptions.Multiline | (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));

    public string Apply(string text)
    {
        return Compiled.Replace(text, Replacement);
    }
}
