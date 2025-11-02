using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.ADT.CelticSpike;

public abstract partial class SharedCelticSpikeSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class ImpaleDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RemoveDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class EscapeDoAfterEvent : SimpleDoAfterEvent
{
}
