using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserInterface.RichText;

public sealed class MonospaceTag : IMarkupTag
{
    public const string MonospaceFont = "Monospace";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "mono";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(
            context.Font,
            node,
            _resourceCache,
            _prototypeManager,
            MonospaceFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
