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
    public ProtoId<TTSVoicePrototype>? VoicePrototypeId = "glados";

    [ViewVariables(VVAccess.ReadWrite)]
    public TTSVoicePrototype? Voice
    {
        get
        {
            if (VoicePrototypeId is not { } id)
                return null;

            return IoCManager.Resolve<IPrototypeManager>().TryIndex(id, out var voice) ? voice : null;
        }
        set => VoicePrototypeId = value?.ID;
    }

    /// <summary>
    /// Service effect the entity constantly speaks through, for example <c>robotic</c>.
    /// Empty means a clean voice.
    /// </summary>
    [DataField]
    public string Effect = string.Empty;
}
