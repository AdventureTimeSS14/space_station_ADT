using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MindSlave;

/// <summary>
/// Shared server/client base for MindSlave systems.
/// </summary>
public abstract class SharedMindSlaveSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class MindSlaveImplantDoAfterEvent : SimpleDoAfterEvent
{
}
