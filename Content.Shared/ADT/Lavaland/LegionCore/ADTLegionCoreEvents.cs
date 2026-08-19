using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland.LegionCore;

[Serializable, NetSerializable]
public sealed partial class ADTLegionCoreApplyDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ADTLegionCoreImplantDoAfterEvent : SimpleDoAfterEvent
{
}
