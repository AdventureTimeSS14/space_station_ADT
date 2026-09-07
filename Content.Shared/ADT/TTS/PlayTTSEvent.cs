using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.TTS;

[Serializable, NetSerializable]
public enum TTSKind : byte
{
    /// <summary>Normal speech, played from its source.</summary>
    World = 0,

    /// <summary>Voice preview in the character editor.</summary>
    Preview = 1,

    /// <summary>Radio speech, played without positional audio.</summary>
    Radio = 2,
}

[Serializable, NetSerializable]
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }

    public NetEntity? SourceUid { get; }

    public bool IsWhisper { get; }

    public TTSKind Kind { get; }

    public ProtoId<RadioChannelPrototype>? Channel { get; }

    public PlayTTSEvent(
        byte[] data,
        NetEntity? sourceUid = null,
        bool isWhisper = false,
        TTSKind kind = TTSKind.World,
        ProtoId<RadioChannelPrototype>? channel = null)
    {
        Data = data;
        SourceUid = sourceUid;
        IsWhisper = isWhisper;
        Kind = kind;
        Channel = channel;
    }
}
