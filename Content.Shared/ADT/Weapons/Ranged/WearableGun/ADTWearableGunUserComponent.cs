using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Ranged.WearableGun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTWearableGunUserComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? Gun;

    [ViewVariables]
    public TimeSpan NextPopup;
}
