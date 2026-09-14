using System.Diagnostics.Contracts;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.RichText;

/// <summary>
///
/// </summary>
public static class MarkupSanitizer
{
    public static readonly IReadOnlySet<string> LabelTags = new HashSet<string>
    {
        "bold",
        "italic",
        "bolditalic",
        "color",
        "mono",
    };

    [Pure]
    public static string SanitizeLabel(string text)
    {
        return Sanitize(text, LabelTags);
    }

    [Pure]
    public static string Sanitize(string text, IReadOnlySet<string> allowedTags)
    {
        var message = FormattedMessage.FromMarkupPermissive(text);

        var sb = new StringBuilder(text.Length);
        foreach (var node in message.Nodes)
        {
            if (node.Name == null || allowedTags.Contains(node.Name))
                sb.Append(node);
        }

        return sb.ToString();
    }
}
