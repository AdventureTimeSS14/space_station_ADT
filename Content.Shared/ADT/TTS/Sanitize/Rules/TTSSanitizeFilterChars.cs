using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TTS.Sanitize;

[DataDefinition]
public sealed partial class TTSSanitizeFilterChars : ITTSSanitizeRule
{
    [DataField(required: true)]
    public string Allowed = string.Empty;

    [DataField]
    public string Replacement = string.Empty;

    private Regex? _regex;

    private Regex Compiled => _regex ??= new Regex($"[^{Allowed}]", RegexOptions.Compiled);

    public string Apply(string text)
    {
        return Compiled.Replace(text, Replacement);
    }
}
