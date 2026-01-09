using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StationRadio.Events;

/// <summary>
/// Event, summoned when music stopped in station radio.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationRadioMediaStoppedEvent : EntityEventArgs
{
    public string ChannelId { get; }

    public StationRadioMediaStoppedEvent(string channelId)
    {
        ChannelId = channelId;
    }
}