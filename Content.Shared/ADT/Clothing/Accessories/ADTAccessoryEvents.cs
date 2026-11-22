using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Clothing.Accessories;

[Serializable, NetSerializable]
public sealed partial class ADTAccessoryAttachDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Raised on an accessory when the clothing it is attached to starts or stops being worn,
/// and when it is attached to or removed from clothing that is being worn.
/// </summary>
[ByRefEvent]
public readonly record struct ADTAccessoryWornChangedEvent(EntityUid Wearer, EntityUid Holder, bool Worn);
