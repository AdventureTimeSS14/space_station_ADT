using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.RPD;

[Serializable, NetSerializable]
public sealed class RPDSystemMessage : BoundUserInterfaceMessage
{
    public ProtoId<RPDPrototype> ProtoId;

    /// <summary>
    /// True if the selected prototype is meant to be the secondary (Alt) configuration.
    /// </summary>
    public bool Secondary;

    public RPDSystemMessage(ProtoId<RPDPrototype> protoId, bool secondary = false)
    {
        ProtoId = protoId;
        Secondary = secondary;
    }
}

[Serializable, NetSerializable]
public sealed class RPDConstructionGhostRotationEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly Direction Direction;

    public RPDConstructionGhostRotationEvent(NetEntity netEntity, Direction direction)
    {
        NetEntity = netEntity;
        Direction = direction;
    }
}

[Serializable, NetSerializable]
public enum RpdUiKey : byte
{
    Key,

    /// <summary>
    /// Secondary (Alt+click) configuration picker.
    /// </summary>
    Secondary
}
