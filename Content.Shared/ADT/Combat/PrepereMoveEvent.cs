using Content.Shared.Actions;

namespace Content.Shared.ADT.Combat;

public sealed partial class PrepareMoveEvent : InstantActionEvent
{
    [DataField]
    public List<IComboEffect> ComboEvents;

    [DataField]
    public string Name;
}
