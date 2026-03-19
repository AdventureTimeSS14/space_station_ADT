using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Fuel;

[Serializable, NetSerializable]
public sealed partial class FuelableInsertDoAfterEvent : SimpleDoAfterEvent
{
}

