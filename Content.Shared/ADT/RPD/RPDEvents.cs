using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.RPD;

[Serializable, NetSerializable]
public sealed class RPDSystemMessage : BoundUserInterfaceMessage
{
    public ProtoId<RPDPrototype> ProtoId;

    public RPDSystemMessage(ProtoId<RPDPrototype> protoId)
    {
        ProtoId = protoId;
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
    Key
}
