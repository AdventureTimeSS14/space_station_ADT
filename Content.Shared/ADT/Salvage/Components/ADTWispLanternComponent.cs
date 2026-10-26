using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTWispLanternComponent : Component
{
    [DataField]
    public bool Released;

    [ViewVariables]
    public EntityUid? User;

    [ViewVariables]
    public EntityUid? Wisp;

    [DataField]
    public EntProtoId WispProto = "ADTWisp";

    [ViewVariables]
    public bool GrantedVision;

    [ViewVariables]
    public bool WasVisionActive;

    [DataField]
    public float StoredRadius = 7f;

    [DataField]
    public float ReleasedRadius = 2f;

    [DataField]
    public string StoredState = "icon";

    [DataField]
    public string ReleasedState = "icon-empty";

    [DataField]
    public SoundSpecifier ReleaseSound = new SoundPathSpecifier("/Audio/Magic/ethereal_exit.ogg");

    [DataField]
    public SoundSpecifier ReturnSound = new SoundPathSpecifier("/Audio/Magic/ethereal_enter.ogg");
}

[Serializable, NetSerializable]
public enum ADTWispLanternVisuals : byte
{
    Released,
}
