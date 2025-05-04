using System.Collections.Generic;
using Content.Shared.ADT.CCVar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserInterface.RichText;

/// <summary>
/// Applies the font provided as the tags parameter to the markup drawing context.
/// Definitely not save for user supplied markup
/// </summary>
/// Да, копипаста робусты, и что?
public sealed class ToggleableFontTag : IMarkupTag
{
    public const string DefaultFont = "Default";
    public const int DefaultSize = 12;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string Name => "tfont";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        string fontId = node.Value.StringValue ?? DefaultFont;

        var font = CreateFont(context.Font, node, _resourceCache, _prototypeManager, _cfg, fontId);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }

    /// <summary>
    /// Creates the a vector font from the supplied font id.<br/>
    /// The size of the resulting font will be either the size supplied as a parameter to the tag, the previous font size or 12
    /// </summary>
    public static Font CreateFont(
        Stack<Font> contextFontStack,
        MarkupNode node,
        IResourceCache cache,
        IPrototypeManager prototypeManager,
        IConfigurationManager cfg,
        string fontId)
    {
        var size = DefaultSize;

        if (contextFontStack.TryPeek(out var previousFont))
        {
            switch (previousFont)
            {
                case VectorFont vectorFont:
                    size = vectorFont.Size;
                    break;
                case StackedFont stackedFont:
                    if (stackedFont.Stack.Length == 0 || stackedFont.Stack[0] is not VectorFont stackVectorFont)
                        break;

                    size = stackVectorFont.Size;
                    break;
            }
        }

        if (cfg.GetCVar(ADTCCVars.EnableLanguageFonts))
        {
            if (node.Attributes.TryGetValue("size", out var sizeParameter))
                size = (int) (sizeParameter.LongValue ?? size);
        }
        else
        {
            fontId = DefaultFont;
            size = DefaultSize;

            if (node.Attributes.TryGetValue("defaultFont", out var dFont) && dFont.TryGetString(out var dFontStr))
                fontId = dFontStr;
            if (node.Attributes.TryGetValue("defaultSize", out var dSize))
                size = (int)(dSize.LongValue ?? DefaultSize);
        }


        if (!prototypeManager.TryIndex<FontPrototype>(fontId, out var prototype))
            prototype = prototypeManager.Index<FontPrototype>(DefaultFont);

        var fontResource = cache.GetResource<FontResource>(prototype.Path);
        return new VectorFont(fontResource, size);
    }
}
