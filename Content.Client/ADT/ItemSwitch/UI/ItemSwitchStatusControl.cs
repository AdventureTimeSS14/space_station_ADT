using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Content.Shared.ADT.ItemSwitch.Components;

namespace Content.Client.ADT.ItemSwitch.UI;

public sealed class ItemSwitchStatusControl : PollingItemStatusControl<ItemSwitchStatusControl.Data>
{
    private readonly Entity<ItemSwitchComponent> _parent;
    private readonly RichTextLabel _label;

    public ItemSwitchStatusControl(Entity<ItemSwitchComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        if (parent.Comp.ShowLabel)
            AddChild(_label);

        UpdateDraw();
    }

    protected override Data PollData()
    {
        string? verb = null;
        if (_parent.Comp.States.TryGetValue(_parent.Comp.State, out var state))
            verb = state.Verb;

        return new Data(_parent.Comp.State, verb);
    }

    protected override void Update(in Data data)
    {
        var stateText = data.Verb is not null && Loc.TryGetString(data.Verb, out var localized)
            ? localized
            : data.State;

        _label.SetMarkup(Loc.GetString("itemswitch-component-on-examine-detailed-message",
            ("state", stateText)));
    }

    public record struct Data(string State, string? Verb);
}
