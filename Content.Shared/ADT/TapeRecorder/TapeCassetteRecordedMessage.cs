using Content.Shared.ADT.Language;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Corvax.TTS;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.TapeRecorder;

/// <summary>
/// Every chat event recorded on a tape is saved in this format
/// </summary>
[ImplicitDataDefinitionForInheritors]
public sealed partial class TapeCassetteRecordedMessage : IComparable<TapeCassetteRecordedMessage>
{
    /// <summary>
    /// Number of seconds since the start of the tape that this event was recorded at
    /// </summary>
    [DataField(required: true)]
    public float Timestamp = 0;

    /// <summary>
    /// The name of the entity that spoke
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// The verb used for this message.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? Verb;

    /// <summary>
    /// What was spoken
    /// </summary>
    [DataField]
    public string Message = string.Empty;

    [DataField]
    public string? Bark;

    [DataField]
    public float BarkPitch;

    [DataField]
    public ProtoId<TTSVoicePrototype>? TTS;

    [DataField]
    public ProtoId<LanguagePrototype>? Language;

    public TapeCassetteRecordedMessage(float timestamp, string name, ProtoId<SpeechVerbPrototype> verb, string? bark, float barkPitch, ProtoId<TTSVoicePrototype>? tts, ProtoId<LanguagePrototype> language, string message)
    {
        Timestamp = timestamp;
        Name = name;
        Verb = verb;
        Bark = bark;
        BarkPitch = barkPitch;
        TTS = tts;
        Language = language;
        Message = message;
    }

    public int CompareTo(TapeCassetteRecordedMessage? other)
    {
        if (other == null)
            return 0;

        return (int) (Timestamp - other.Timestamp);
    }
}
