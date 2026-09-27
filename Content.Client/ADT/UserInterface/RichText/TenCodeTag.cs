using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.TenCodes;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserInterface.RichText;

public sealed class TenCodeTag : IMarkupTagHandler
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public static readonly Color HighlightColor = Color.FromHex("#45E0FF");

    private const string FontPath = "/Fonts/NotoSans/NotoSans-Bold.ttf";
    private const int FontSize = 12;

    public string Name => ADTTenCodes.MarkupTag;

    public string TextBefore(MarkupNode node)
    {
        if (!TryGetCode(node, out var code) || KnowsTenCodes())
            return string.Empty;

        return code;
    }

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!TryGetCode(node, out var code) || !KnowsTenCodes())
            return false;

        var label = new Label
        {
            Text = code,
            FontColorOverride = HighlightColor,
            FontOverride = new VectorFont(_cache.GetResource<FontResource>(FontPath), FontSize),
            MouseFilter = Control.MouseFilterMode.Stop,
        };

        if (_prototype.TryIndex<ADTTenCodePrototype>(code, out var proto))
            label.ToolTip = Loc.GetString(proto.Description);

        control = label;
        return true;
    }

    private static bool TryGetCode(MarkupNode node, [NotNullWhen(true)] out string? code)
    {
        code = node.Value.StringValue;
        return !string.IsNullOrEmpty(code);
    }

    private bool KnowsTenCodes()
    {
        return _player.LocalEntity is { } player && _entity.HasComponent<ADTTenCodeKnowledgeComponent>(player);
    }
}
