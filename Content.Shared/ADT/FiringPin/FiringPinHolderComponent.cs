using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.FiringPin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FiringPinHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "firing_pin";

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public bool RequiresPin;

    [DataField, AutoNetworkedField]
    public bool Emagged;

    [DataField]
    public EntProtoId? StartingPin;

    [DataField]
    public float RemovalDelay = 5f;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/pistol_magin.ogg");

    [DataField]
    public SoundSpecifier? RemoveSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/pistol_magout.ogg");
}