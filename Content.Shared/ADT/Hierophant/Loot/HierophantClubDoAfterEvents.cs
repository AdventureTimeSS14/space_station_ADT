using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Hierophant.Loot;

[Serializable, NetSerializable]
public sealed partial class HierophantBeaconDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class HierophantTeleportDoAfterEvent : SimpleDoAfterEvent
{
}
