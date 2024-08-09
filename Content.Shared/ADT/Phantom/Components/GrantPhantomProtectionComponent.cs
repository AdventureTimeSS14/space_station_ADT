using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Phantom;

[RegisterComponent]
public sealed partial class GrantPhantomProtectionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool WorkInHand = false;
}
