using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SpeechBarks;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlaySpeechBarksEvent : EntityEventArgs
{
    public NetEntity? Source;
    public string? Message;
    public string Sound;
    // public string ExclaimSound;
    // public string AskSound;
    public float Pitch;
    public float LowVar;
    public float HighVar;
    public bool IsWhisper;
    public PlaySpeechBarksEvent(
        NetEntity source,
        string? message,
        string sound,
        // string exclaimSound,
        // string askSound,
        float pitch,
        float lowVar,
        float highVar,
        bool isWhisper)
    {
        Source = source;
        Message = message;
        Sound = sound;
        // ExclaimSound = exclaimSound;
        // AskSound = askSound;
        Pitch = pitch;
        LowVar = lowVar;
        HighVar = highVar;
        IsWhisper = isWhisper;
    }
}
