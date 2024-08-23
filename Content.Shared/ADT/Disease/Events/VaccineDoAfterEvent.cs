using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Disease.Events;

[Serializable, NetSerializable]
public sealed partial class VaccineDoAfterEvent : SimpleDoAfterEvent
{
}
