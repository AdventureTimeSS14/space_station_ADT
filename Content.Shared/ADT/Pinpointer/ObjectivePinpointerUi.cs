using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Pinpointer;

[Serializable, NetSerializable]
public enum ObjectivePinpointerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ObjectivePinpointerBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<ObjectivePinpointerTarget> Targets;

    public ObjectivePinpointerBoundUserInterfaceState(List<ObjectivePinpointerTarget> targets)
    {
        Targets = targets;
    }
}

[Serializable, NetSerializable]
public sealed class ObjectivePinpointerSelectMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public ObjectivePinpointerSelectMessage(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class ObjectivePinpointerTarget
{
    public NetEntity Entity;
    public string DisplayName;
    public ObjectivePinpointerTargetType Type;

    public ObjectivePinpointerTarget(NetEntity entity, string displayName, ObjectivePinpointerTargetType type)
    {
        Entity = entity;
        DisplayName = displayName;
        Type = type;
    }
}

[Serializable, NetSerializable]
public enum ObjectivePinpointerTargetType : byte
{
    Steal,
    Kill,
    Protect
}