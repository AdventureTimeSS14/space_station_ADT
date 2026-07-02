using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Paper;

/// <summary>
///     Markup tag handler for <c>[field]</c> placeholders in paper documents.
///     Renders as a localized placeholder string (e.g. <c>[____]</c>) in read mode.
///     In write mode, the client-side PaperWindow UI replaces these with interactive
///     buttons that open a popup menu for selecting an autofill value from PaperFieldContext.
/// </summary>
public sealed class FieldTag : IMarkupTag
{
    public string Name => "field";

    public string TextBefore(MarkupNode node)
    {
        return Loc.GetString("paper-field-placeholder");
    }
}
