using Robust.Shared.Serialization;

namespace Content.Shared.ADT.TTS;

[Serializable, NetSerializable]
public sealed class RequestPreviewTTSEvent(string voiceId) : EntityEventArgs
{
    public string VoiceId { get; } = voiceId;
}
