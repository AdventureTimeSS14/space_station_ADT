using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TTS;

/// <summary>
/// Voices the entity's chat messages through speech synthesis.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Voice the entity speaks with.
    /// </summary>
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype>? VoicePrototypeId;

    /// <summary>
    /// Service effect the entity constantly speaks through, for example <c>robotic</c>.
    /// Null means a clean voice.
    /// </summary>
    [DataField]
    public string? Effect;
}
