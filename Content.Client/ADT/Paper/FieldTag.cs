using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Paper;

public sealed class FieldTag : IMarkupTag
{
    public string Name => "field";

    public string TextBefore(MarkupNode node)
    {
        return Loc.GetString("paper-field-placeholder");
    }
}
