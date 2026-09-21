using System.Text.RegularExpressions;

namespace Content.Client.ADT.Chat;

/// <summary>
/// Разбор пользовательских замен "триггер=эмоут" и их применение к сообщению.
/// </summary>
public static class CustomEmoteParser
{
    private const char Separator = '=';

    public static List<(Regex Regex, string Emote)> Parse(string raw)
    {
        var result = new List<(Regex, string)>();

        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var sep = line.IndexOf(Separator);
            if (sep <= 0 || sep == line.Length - 1)
                continue;

            var trigger = line[..sep].Trim();
            var emote = line[(sep + 1)..].Trim();

            if (trigger.Length == 0 || emote.Length == 0)
                continue;

            result.Add((BuildTriggerRegex(trigger), emote));
        }

        return result;
    }

    /// <summary>
    /// Триггер должен стоять отдельным словом: в начале строки или после пробела,
    /// и перед знаком препинания, пробелом или концом строки.
    /// </summary>
    public static Regex BuildTriggerRegex(string trigger)
    {
        var escaped = Regex.Escape(trigger);

        return new Regex(
            $@"\s{escaped}(?=\p{{P}}|\s|$)|^{escaped}(?:\p{{P}}|(?=\s|$))",
            RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Вырезает триггеры из сообщения. Если их несколько, берётся самый правый,
    /// как в серверном чатсане.
    /// </summary>
    public static bool TryApply(
        IReadOnlyList<(Regex Regex, string Emote)> entries,
        string text,
        out string cleaned,
        out string? emote)
    {
        emote = null;
        cleaned = text;

        var lastIndex = -1;

        foreach (var (regex, candidate) in entries)
        {
            var match = regex.Match(cleaned);

            if (!match.Success)
                continue;

            if (match.Index > lastIndex)
            {
                lastIndex = match.Index;
                emote = candidate;
            }

            cleaned = regex.Replace(cleaned, string.Empty);
        }

        cleaned = cleaned.Trim();
        return emote is not null;
    }
}
