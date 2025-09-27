using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.SpeechBarks;

[Serializable, NetSerializable]
public sealed class PlaySpeechBarksEvent : EntityEventArgs
{
    public NetEntity? Source;
    public string? Message;
    public SoundSpecifier SoundSpecifier;
    public float Pitch;
    public float LowVar;
    public float HighVar;
    public bool IsWhisper;

    public PlaySpeechBarksEvent(
        NetEntity source,
        string? message,
        SoundSpecifier soundSpecifier,
        float pitch,
        float lowVar,
        float highVar,
        bool isWhisper)
    {
        Source = source;
        Message = message;
        SoundSpecifier = soundSpecifier;
        Pitch = pitch;
        LowVar = lowVar;
        HighVar = highVar;
        IsWhisper = isWhisper;
    }
}