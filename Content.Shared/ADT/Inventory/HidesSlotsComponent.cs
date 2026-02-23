using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Inventory;

[RegisterComponent, NetworkedComponent]
public sealed partial class HidesSlotsComponent : Component
{
    [DataField("hidesSlots")]
    public SlotFlags HidesSlots = SlotFlags.NONE;
}
