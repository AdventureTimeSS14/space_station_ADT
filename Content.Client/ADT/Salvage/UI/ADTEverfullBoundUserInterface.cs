using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Salvage.Components;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Salvage.UI;

public sealed class ADTEverfullBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SimpleRadialMenu? _menu;

    public ADTEverfullBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<ADTEverfullComponent>(Owner, out var everfull))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        var options = new List<RadialMenuOptionBase>(everfull.Options.Count);

        for (var i = 0; i < everfull.Options.Count; i++)
        {
            var option = everfull.Options[i];
            var index = i;

            options.Add(new RadialMenuActionOption<int>(SendSelect, index)
            {
                ToolTip = Loc.GetString(option.Name),
                IconSpecifier = RadialMenuIconSpecifier.With(option.Icon),
            });
        }

        _menu.SetButtons(options);
        _menu.OpenOverMouseScreenPosition();
    }

    private void SendSelect(int index)
    {
        SendPredictedMessage(new ADTEverfullSelectMessage(index));
    }
}
