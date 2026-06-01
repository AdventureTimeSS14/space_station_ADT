using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.CCVar;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserInterface.RichText;
public sealed class IconTag : IMarkupTag
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private SpriteSystem? _spriteSystem;

    private const int IconSize = 20;

    public string Name => "icon";

    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!_cfg.GetCVar(ADTCCVars.EnableJobIconAnimation) || !_cfg.GetCVar(ADTCCVars.EnableChatJobIcons))
            return false;

        if (!node.Attributes.TryGetValue("src", out var id) || id.StringValue == null)
            return false;

        _spriteSystem ??= _entitySystem.GetEntitySystem<SpriteSystem>();

        if (!_prototype.TryIndex<JobIconPrototype>(id.StringValue, out var jobProto))
            return false;

        var spec = jobProto.Icon;

        control = CreateIconControl(spec);

        if (control != null && node.Attributes.TryGetValue("tooltip", out var tooltip) && tooltip.StringValue != null)
            control.ToolTip = tooltip.StringValue;

        return control != null;
    }

    private Control? CreateIconControl(SpriteSpecifier spec)
    {
        try
        {
            var state = _spriteSystem!.RsiStateLike(spec);

            if (state.IsAnimated)
                return CreateAnimatedIcon(spec);

            return CreateStaticIcon(spec);
        }
        catch
        {
            return CreateStaticIconSafe(spec);
        }
    }

    private static AnimatedTextureRect CreateAnimatedIcon(SpriteSpecifier spec)
    {
        var anim = new AnimatedTextureRect();
        anim.SetFromSpriteSpecifier(spec);
        anim.DisplayRect.SetWidth = IconSize;
        anim.DisplayRect.SetHeight = IconSize;
        anim.DisplayRect.Stretch = TextureRect.StretchMode.Scale;
        anim.MouseFilter = Control.MouseFilterMode.Stop;
        return anim;
    }

    private TextureRect CreateStaticIcon(SpriteSpecifier spec)
    {
        var texture = _spriteSystem!.Frame0(spec);
        return CreateTextureControl(texture);
    }

    private TextureRect? CreateStaticIconSafe(SpriteSpecifier spec)
    {
        try
        {
            var texture = _spriteSystem!.Frame0(spec);
            return CreateTextureControl(texture);
        }
        catch
        {
            return null;
        }
    }

    private static TextureRect CreateTextureControl(Texture texture)
    {
        return new TextureRect
        {
            Texture = texture,
            SetWidth = IconSize,
            SetHeight = IconSize,
            Stretch = TextureRect.StretchMode.Scale,
            MouseFilter = Control.MouseFilterMode.Stop,
        };
    }
}
