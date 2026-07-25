using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using Content.Client.ADTUserInterface.RichText;
using Content.Shared.ADT.Utility;

namespace Content.Client.ADT.UserInterface.RichText;

public sealed class EntityTextureTag : BaseTextureTag, IMarkupTagHandler
{
    public string Name => "enttex";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("id", out var idParameter) || !MarkupHelpers.TryGetLong(idParameter, out var id))
            return false;

        if (!node.Attributes.TryGetValue("size", out var size) || !MarkupHelpers.TryGetLong(size, out var sizeValue))
        {
            sizeValue = 32;
        }

        if (!node.Attributes.TryGetValue("scale", out var scale) || !MarkupHelpers.TryGetLong(scale, out var scaleValue))
        {
            scaleValue = 2;
        }

        if (!node.Attributes.TryGetValue("offsetX", out var xParameter) || !MarkupHelpers.TryGetLong(xParameter, out var x))
            x = 0;

        if (!node.Attributes.TryGetValue("offsetY", out var yParameter) || !MarkupHelpers.TryGetLong(yParameter, out var y))
            y = 0;

        if (!TryDrawIconEntity(new NetEntity((int) id), sizeValue.Value, scaleValue.Value, new Vector2((float) x, (float) y), out var texture))
            return false;

        control = texture;

        return true;
    }
}