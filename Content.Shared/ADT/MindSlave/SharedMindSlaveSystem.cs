using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MindSlave;

public abstract class SharedMindSlaveSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class MindSlaveImplantDoAfterEvent : SimpleDoAfterEvent
{
}
