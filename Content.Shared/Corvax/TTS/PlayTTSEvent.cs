using Content.Shared.ADT.Language;  // ADT Languages
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }
    public byte[] LanguageData { get; }
    public NetEntity? SourceUid { get; }
    public bool IsWhisper { get; }
    public string LanguageProtoId { get; }  // ADT Languages
    public PlayTTSEvent(byte[] data, byte[] languageData, LanguagePrototype language, NetEntity? sourceUid = null, bool isWhisper = false)  // ADT Languages
    {
        Data = data;
        SourceUid = sourceUid;
        IsWhisper = isWhisper;
        LanguageProtoId = language.ID;  // ADT Languages
        LanguageData = languageData;    // ADT Languages
    }
}
