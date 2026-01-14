using Robust.Shared.Serialization;
namespace Content.Shared.ADT.StationRadio.Events;
[Serializable, NetSerializable]
public sealed class StationRadioChannelChangedEvent : EntityEventArgs
{
    public string NewChannelId { get; }
    public StationRadioChannelChangedEvent(string newChannelId)
    {
        NewChannelId = newChannelId;
    }
}
