using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StationRadio.Events;

[Serializable, NetSerializable]
public sealed class StationRadioMediaStoppedEvent : EntityEventArgs
{
    public StationRadioMediaStoppedEvent()
    {

    }
}