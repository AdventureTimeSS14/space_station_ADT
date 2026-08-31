using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TTS.Sanitize;

[DataDefinition]
public sealed partial class TTSSanitizeCharMap : ITTSSanitizeRule
{
    [DataField]
    public string Pattern = "[a-zA-Z]";

    [DataField(required: true)]
    public Dictionary<string, string> Map = new();

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
        return Map.TryGetValue(match.Value.ToLowerInvariant(), out var replacement)
            ? replacement
            : match.Value;
    }
}
