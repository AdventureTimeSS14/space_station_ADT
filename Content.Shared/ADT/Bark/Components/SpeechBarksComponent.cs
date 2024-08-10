using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SpeechBarks;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechBarksComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<BarkPrototype>? BarkPrototype;

    [DataField]
    public string Sound = "/Audio/Voice/Talk/speak_1.ogg";

    // [DataField]
    // public string ExclaimSound = "/Audio/Voice/Talk/speak_1_exclaim.ogg";

    // [DataField]
    // public string AskSound = "/Audio/Voice/Talk/speak_1_ask.ogg";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BarkPitch = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BarkLowVar = 0.1f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BarkHighVar = 0.5f;
}

