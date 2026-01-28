using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StationRadio.Events;

[Serializable, NetSerializable]
public sealed class StationRadioMediaPlayedEvent : EntityEventArgs
{
    public SoundPathSpecifier MediaPlayed { get; }
    public string ChannelId { get; }
    public TimeSpan StartTime { get; }
    public Guid BroadcastId { get; }

    public StationRadioMediaPlayedEvent(SoundPathSpecifier media, string channelId, TimeSpan startTime, Guid broadcastId)
    {
        MediaPlayed = media;
        ChannelId = channelId;
        StartTime = startTime;
        BroadcastId = broadcastId;
    }
}