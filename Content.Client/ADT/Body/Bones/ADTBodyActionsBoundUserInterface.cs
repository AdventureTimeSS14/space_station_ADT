using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Body.Bones;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Body.Bones;

public sealed class ADTBodyActionsBoundUserInterface : BoundUserInterface
{
    private static readonly SpriteSpecifier SplintIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/ADT/Objects/Specific/Medical/splints.rsi"), "medical");
    private static readonly SpriteSpecifier RemoveIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));
    private static readonly SpriteSpecifier ReattachIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/plus.svg.192dpi.png"));
    private static readonly SpriteSpecifier ShrapnelIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/ADT/Objects/Weapons/shrapnel.rsi"), "shrapnel1");
    private static readonly SpriteSpecifier ToolIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png"));

    [ViewVariables]
    private SimpleRadialMenu? _menu;

    public ADTBodyActionsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null || state is not ADTBodyActionsBuiState actions)
            return;

        var buttons = new List<RadialMenuOptionBase>();
        foreach (var group in actions.Options.GroupBy(o => o.Organ))
        {
            var organ = EntMan.GetEntity(group.Key);
            var options = group.ToList();

            if (options.Count == 1)
            {
                buttons.Add(ActionButton(options[0], RadialMenuIconSpecifier.With(organ)));
                continue;
            }

            var nested = options
                .Select(option => (RadialMenuOptionBase) ActionButton(option, RadialMenuIconSpecifier.With(GetIcon(option.Action))))
                .ToList();

            buttons.Add(new RadialMenuNestedLayerOption(nested)
            {
                ToolTip = options[0].Limb,
                IconSpecifier = RadialMenuIconSpecifier.With(organ),
            });
        }

        _menu.SetButtons(buttons);
    }

    private RadialMenuActionOption<ADTBodyActionOption> ActionButton(ADTBodyActionOption option, RadialMenuIconSpecifier? icon)
    {
        return new RadialMenuActionOption<ADTBodyActionOption>(Select, option)
        {
            ToolTip = option.Label,
            IconSpecifier = icon,
        };
    }

    private void Select(ADTBodyActionOption option)
    {
        SendMessage(new ADTBodyActionSelectMessage(option.Organ, option.Action));
        Close();
    }

    private static SpriteSpecifier GetIcon(ADTBodyAction action)
    {
        return action switch
        {
            ADTBodyAction.ApplySplint => SplintIcon,
            ADTBodyAction.RemoveSplint => RemoveIcon,
            ADTBodyAction.Reattach => ReattachIcon,
            ADTBodyAction.RemoveShrapnelHands => ShrapnelIcon,
            _ => ToolIcon,
        };
    }
}
