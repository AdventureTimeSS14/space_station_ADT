using Robust.Shared.GameStates;
using Content.Shared.Inventory;

namespace Content.Shared.ADT.Inventory;

[RegisterComponent, NetworkedComponent]
public sealed partial class HidesSlotsComponent : Component
{
    [DataField("hidesSlots")]
    public SlotFlags HidesSlots = SlotFlags.NONE;
}
