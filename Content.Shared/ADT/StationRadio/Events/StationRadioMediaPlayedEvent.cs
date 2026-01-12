using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StationRadio.Events;

[Serializable, NetSerializable]
public sealed class StationRadioMediaPlayedEvent : EntityEventArgs
{
    public SoundPathSpecifier MediaPlayed { get; }
    public string ChannelId { get; }

    public StationRadioMediaPlayedEvent(SoundPathSpecifier media, string channelId)
    {
        MediaPlayed = media;
        ChannelId = channelId;
    }
}