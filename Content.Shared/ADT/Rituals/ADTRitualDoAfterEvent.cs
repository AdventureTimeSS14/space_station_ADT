using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Rituals;

[Serializable, NetSerializable]
public sealed partial class ADTRitualDoAfterEvent : SimpleDoAfterEvent
{
}
