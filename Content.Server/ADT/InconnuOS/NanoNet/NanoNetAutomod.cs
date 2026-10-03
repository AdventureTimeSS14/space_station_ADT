using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.ADT.InconnuOS;

namespace Content.Server.ADT.InconnuOS.NanoNet;

public readonly record struct NanoNetVerdict(OsValidationError Error, string Detail, string Match)
{
    public static readonly NanoNetVerdict Clean = new(OsValidationError.None, string.Empty, string.Empty);

    public bool Rejected => Error != OsValidationError.None;
}

public sealed class NanoNetAutomod
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex ForbiddenTag = new(
        @"<\s*/?\s*(iframe|frameset|frame|object|embed|applet|base|meta|link|portal)(?![a-z0-9-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex ScriptTag = new(
        @"<\s*script(?![a-z0-9-])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex EventHandler = new(
        @"(?<![a-z0-9_-])on[a-z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly string[] ScriptSchemes = { "javascript:", "vbscript:", "data:text/html" };

    private static readonly Regex Tags = new(@"<[^>]*>", RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Dictionary<char, char> Homoglyphs = new()
    {
        ['a'] = 'а', ['b'] = 'в', ['c'] = 'с', ['e'] = 'е', ['h'] = 'н', ['k'] = 'к', ['m'] = 'м',
        ['o'] = 'о', ['p'] = 'р', ['t'] = 'т', ['x'] = 'х', ['y'] = 'у',
        ['0'] = 'о', ['1'] = 'i', ['3'] = 'з', ['4'] = 'ч', ['6'] = 'б', ['@'] = 'а',
        ['ё'] = 'е', ['й'] = 'и',
    };

    private readonly List<string> _words = new();
    private readonly HashSet<string> _wholeWords = new();
    private readonly List<string> _phrases = new();
    private readonly List<string> _falsePositives = new();
    private readonly List<Regex> _patterns = new();

    public NanoNetAutomod(IEnumerable<NanoNetFilterPrototype> filters, ISawmill sawmill)
    {
        foreach (var filter in filters)
        {
            AddNormalized(_words, Decode(filter.Words, filter.ID, sawmill));
            AddNormalized(_falsePositives, Decode(filter.FalsePositives, filter.ID, sawmill));
            AddNormalized(_phrases, Decode(filter.Phrases, filter.ID, sawmill));

            foreach (var word in Decode(filter.WholeWords, filter.ID, sawmill))
            {
                var normalized = Normalize(word);
                if (normalized.Length > 0)
                    _wholeWords.Add(normalized);
            }

            foreach (var pattern in Decode(filter.Patterns, filter.ID, sawmill))
            {
                try
                {
                    _patterns.Add(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout));
                }
                catch (ArgumentException e)
                {
                    sawmill.Error($"Invalid pattern in {filter.ID}: {e.Message}");
                }
            }
        }
    }

    private static List<string> Decode(List<string> base64Entries, string filterId, ISawmill sawmill)
    {
        var result = new List<string>(base64Entries.Count);

        foreach (var entry in base64Entries)
        {
            try
            {
                result.Add(Encoding.UTF8.GetString(Convert.FromBase64String(entry)));
            }
            catch (FormatException)
            {
                sawmill.Error($"Invalid base64 entry in {filterId}");
            }
        }

        return result;
    }

    public NanoNetVerdict CheckLabel(string label)
    {
        return TryFindBannedContent(label, out var match)
            ? new NanoNetVerdict(OsValidationError.SiteRejected, string.Empty, match)
            : NanoNetVerdict.Clean;
    }

    public NanoNetVerdict CheckSite(string html, bool allowScripts)
    {
        try
        {
            var decoded = DecodeEntities(html);

            var tag = ForbiddenTag.Match(decoded);
            if (tag.Success)
            {
                var name = tag.Groups[1].Value.ToLowerInvariant();
                return new NanoNetVerdict(OsValidationError.SiteForbiddenMarkup, $"<{name}>", tag.Value);
            }

            if (!allowScripts && TryFindScript(decoded, out var script))
                return new NanoNetVerdict(OsValidationError.SiteScriptsForbidden, string.Empty, script);

            var visible = DecodeEntities(Tags.Replace(html, string.Empty));

            if (TryFindBannedContent(visible, out var match) || TryFindBannedContent(decoded, out match))
                return new NanoNetVerdict(OsValidationError.SiteRejected, string.Empty, match);

            return NanoNetVerdict.Clean;
        }
        catch (RegexMatchTimeoutException)
        {
            return new NanoNetVerdict(OsValidationError.SiteRejected, string.Empty, "regex timeout");
        }
    }

    private static bool TryFindScript(string decoded, [NotNullWhen(true)] out string? match)
    {
        var script = ScriptTag.Match(decoded);
        if (script.Success)
        {
            match = script.Value;
            return true;
        }

        var handler = EventHandler.Match(decoded);
        if (handler.Success)
        {
            match = handler.Value;
            return true;
        }

        var compact = new StringBuilder(decoded.Length);
        foreach (var c in decoded)
        {
            if (c > ' ')
                compact.Append(char.ToLowerInvariant(c));
        }

        var flat = compact.ToString();

        foreach (var scheme in ScriptSchemes)
        {
            if (flat.Contains(scheme, StringComparison.Ordinal))
            {
                match = scheme;
                return true;
            }
        }

        match = null;
        return false;
    }

    private bool TryFindBannedContent(string text, [NotNullWhen(true)] out string? match)
    {
        foreach (var pattern in _patterns)
        {
            var result = pattern.Match(text);
            if (result.Success)
            {
                match = result.Value;
                return true;
            }
        }

        var joined = Normalize(text);

        foreach (var phrase in _phrases)
        {
            if (joined.Contains(phrase, StringComparison.Ordinal))
            {
                match = phrase;
                return true;
            }
        }

        var spelled = new StringBuilder();

        foreach (var token in text.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries))
        {
            var normalized = Normalize(token);

            if (normalized.Length == 1)
            {
                spelled.Append(normalized);
                continue;
            }

            if (CheckToken(spelled.ToString(), out match) || CheckToken(normalized, out match))
                return true;

            spelled.Clear();
        }

        return CheckToken(spelled.ToString(), out match);
    }

    private bool CheckToken(string token, [NotNullWhen(true)] out string? match)
    {
        match = null;

        if (token.Length == 0)
            return false;

        if (_wholeWords.Contains(token))
        {
            match = token;
            return true;
        }

        foreach (var word in _words)
        {
            var index = token.IndexOf(word, StringComparison.Ordinal);

            while (index >= 0)
            {
                if (!IsFalsePositive(token, index, word.Length))
                {
                    match = word;
                    return true;
                }

                index = token.IndexOf(word, index + 1, StringComparison.Ordinal);
            }
        }

        return false;
    }

    private bool IsFalsePositive(string token, int start, int length)
    {
        foreach (var allowed in _falsePositives)
        {
            var index = token.IndexOf(allowed, StringComparison.Ordinal);

            while (index >= 0)
            {
                if (index <= start && index + allowed.Length >= start + length)
                    return true;

                index = token.IndexOf(allowed, index + 1, StringComparison.Ordinal);
            }
        }

        return false;
    }

    private static void AddNormalized(List<string> target, List<string> source)
    {
        foreach (var word in source)
        {
            var normalized = Normalize(word);
            if (normalized.Length > 0 && !target.Contains(normalized))
                target.Add(normalized);
        }
    }

    public static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (var raw in text)
        {
            var c = char.ToLowerInvariant(raw);

            if (Homoglyphs.TryGetValue(c, out var mapped))
                c = mapped;

            if (char.IsLetter(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static string DecodeEntities(string value)
    {
        if (!value.Contains('&'))
            return value;

        var sb = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '&' && (TryDecodeNumeric(value, i, sb, out var consumed) || TryDecodeNamed(value, i, sb, out consumed)))
            {
                i += consumed - 1;
                continue;
            }

            sb.Append(value[i]);
        }

        return sb.ToString();
    }

    private static bool TryDecodeNumeric(string value, int i, StringBuilder sb, out int consumed)
    {
        consumed = 0;

        if (i + 2 >= value.Length || value[i + 1] != '#')
            return false;

        var hex = value[i + 2] is 'x' or 'X';
        var end = i + (hex ? 3 : 2);
        var start = end;
        long code = 0;

        while (end < value.Length)
        {
            var digit = hex ? HexValue(value[end]) : value[end] is >= '0' and <= '9' ? value[end] - '0' : -1;
            if (digit < 0)
                break;

            code = Math.Min(code * (hex ? 16 : 10) + digit, 0x110000);
            end++;
        }

        if (end == start)
            return false;

        if (end < value.Length && value[end] == ';')
            end++;

        if (code is <= 0 or >= 0x110000 or >= 0xD800 and <= 0xDFFF)
            code = 0xFFFD;

        sb.Append(char.ConvertFromUtf32((int) code));
        consumed = end - i;
        return true;
    }

    private static int HexValue(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };
    }

    private static bool TryDecodeNamed(string value, int i, StringBuilder sb, out int consumed)
    {
        consumed = 0;

        var semicolon = value.IndexOf(';', i);
        if (semicolon < 0 || semicolon - i > 10)
            return false;

        var decoded = value[(i + 1)..semicolon].ToLowerInvariant() switch
        {
            "amp" => "&",
            "lt" => "<",
            "gt" => ">",
            "quot" => "\"",
            "apos" => "'",
            "nbsp" => " ",
            "colon" => ":",
            "sol" => "/",
            "lpar" => "(",
            "rpar" => ")",
            "tab" => "\t",
            "newline" => "\n",
            _ => null,
        };

        if (decoded == null)
            return false;

        sb.Append(decoded);
        consumed = semicolon - i + 1;
        return true;
    }
}
