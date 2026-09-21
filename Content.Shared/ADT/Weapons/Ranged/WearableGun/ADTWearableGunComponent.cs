using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Ranged.WearableGun;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTWearableGunComponent : Component
{
    [DataField]
    public string Slot = "gloves";

    [DataField]
    public LocId BlockPopup = "adt-wearable-gun-blocked";

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(2);
}
