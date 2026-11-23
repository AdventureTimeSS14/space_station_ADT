using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.ADT.Mech.EntitySystems;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Mech.UI;

public sealed class MechMagazineStatusControl : PollingItemStatusControl<MechMagazineStatusControl.Data>
{
    private readonly Entity<MechMagazineComponent> _parent;
    private readonly MechGunReloadSystem _reload;
    private readonly RichTextLabel _label;

    public MechMagazineStatusControl(Entity<MechMagazineComponent> parent, MechGunReloadSystem reload)
    {
        _parent = parent;
        _reload = reload;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        return new Data(_reload.GetRounds(_parent.Comp), _parent.Comp.Capacity);
    }

    protected override void Update(in Data data)
    {
        _label.SetMarkup(Loc.GetString("adt-mech-magazine-status", ("rounds", data.Rounds), ("capacity", data.Capacity)));
    }

    public readonly record struct Data(int Rounds, int Capacity);
}
