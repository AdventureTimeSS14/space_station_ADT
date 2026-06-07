using Content.Shared.ADT.Language;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.TTS;

[Serializable, NetSerializable]
public sealed class PlayRadioTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }
    public byte[] LanguageData { get; }
    public string LanguageProtoId { get; }

    public PlayRadioTTSEvent(byte[] data, byte[] languageData, string language)
    {
        Data = data;
        LanguageProtoId = language;
        LanguageData = languageData;
    }
}
