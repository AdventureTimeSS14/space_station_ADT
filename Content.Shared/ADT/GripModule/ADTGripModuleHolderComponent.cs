using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.GripModule;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTGripModuleHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "adt_grip_module";

    [DataField]
    public float RemovalDelay = 5f;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/pistol_magin.ogg");

    [DataField]
    public SoundSpecifier? RemoveSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/pistol_magout.ogg");
}
