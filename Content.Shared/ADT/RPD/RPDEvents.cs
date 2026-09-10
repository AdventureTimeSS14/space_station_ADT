using Content.Shared.Atmos.Components;
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
public sealed class RPDConstructionGhostLayerEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly AtmosPipeLayer Layer;

    public RPDConstructionGhostLayerEvent(NetEntity netEntity, AtmosPipeLayer layer)
    {
        NetEntity = netEntity;
        Layer = layer;
    }
}

public sealed class RPDInstantPlacementEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;
    public readonly EntProtoId BeamPrototype;

    public RPDInstantPlacementEvent(EntityUid user, EntityUid target, EntProtoId beamPrototype)
    {
        User = user;
        Target = target;
        BeamPrototype = beamPrototype;
    }
}

public sealed class RPDPlacementValidatedEvent : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid User;
    public bool Rejected;

    public RPDPlacementValidatedEvent(EntityUid entity, EntityUid user)
    {
        Entity = entity;
        User = user;
    }
}

[Serializable, NetSerializable]
public enum RpdUiKey : byte
{
    Key
}
