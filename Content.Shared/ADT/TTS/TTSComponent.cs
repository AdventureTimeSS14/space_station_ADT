using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.TTS;

[RegisterComponent, NetworkedComponent]
public sealed partial class TTSComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice", customTypeSerializer: typeof(PrototypeIdSerializer<TTSVoicePrototype>))]
    public string? VoicePrototypeId { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("effect")]
    public string? Effect { get; set; }
}
