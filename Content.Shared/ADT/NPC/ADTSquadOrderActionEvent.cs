using Content.Shared.Actions;

namespace Content.Shared.ADT.NPC;

public sealed partial class ADTSquadOrderActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ADTSquadOrder Order;
}
