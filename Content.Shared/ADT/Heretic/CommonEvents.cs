using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Heretic;

[Serializable, NetSerializable]
public sealed class ButtonTagPressedEvent(string id, NetEntity user, NetCoordinates coords) : EntityEventArgs
{
    public NetEntity User = user;

    public NetCoordinates Coords = coords;

    public string Id = id;
}

[ByRefEvent]
public record struct HereticCheckEvent(EntityUid Uid, bool Result = false);

[ByRefEvent]
public record struct GetVirtualItemBlockingEntityEvent(EntityUid Uid);

// ADT: from Goob Wizard SharedSpellsSystem
[Serializable, NetSerializable]
public sealed class StopTargetingEvent : EntityEventArgs;
