// taken and adapted from https://github.com/RMC-14/RMC-14?ysclid=lzx00zxd6e53093995

using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionToggleNinjaNightVision";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public bool Toggleable = true;

    // Only allows for a single slotflag right now because some code uses strings and some code uses enums to determine slots :(
    [DataField, AutoNetworkedField]
    public SlotFlags SlotFlags { get; set; } = SlotFlags.EYES;
}
