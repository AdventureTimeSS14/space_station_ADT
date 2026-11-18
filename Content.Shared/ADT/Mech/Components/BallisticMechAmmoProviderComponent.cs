using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BallisticMechAmmoProviderComponent : MechAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true)]
    public EntProtoId Prototype = default!;

    [DataField, AutoNetworkedField]
    public int Shots = 30;

    [DataField]
    public int Capacity = 30;

    [DataField]
    public float ReloadTime = 10f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ReloadEnd;

    [DataField, AutoNetworkedField]
    public int PendingRounds;

    [DataField]
    public string AmmoContainerId = "storagebase";

    [DataField]
    public string AmmoType = "lightrifle";

    [DataField]
    public SoundSpecifier NoAmmoForReload = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg", new AudioParams().WithVolume(-3f));

    [DataField]
    public SoundSpecifier ReloadSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");
}
