using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Clothing.Accessories;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTAccessoryComponent : Component
{
    [DataField]
    public bool AllowDuplicates = true;

    [DataField]
    public ResPath? Sprite;

    [DataField, AutoNetworkedField]
    public string EquippedState = "equipped-ACCESSORY";

    [DataField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(4);

    [DataField]
    public SlotFlags ActionSlots = SlotFlags.INNERCLOTHING;
}
