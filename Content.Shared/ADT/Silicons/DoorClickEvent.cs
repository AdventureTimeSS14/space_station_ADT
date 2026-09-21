using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Silicons;
public enum DoorClickAction : byte
{
    /// <summary> Включить или выключить аварийный доступ </summary>
    EmergencyAccess,
    /// <summary> Поднять или опустить болты </summary>
    Bolt,
    /// <summary> Включить или выключить электрификацию</summary>
    Electrify,
}

[Serializable, NetSerializable]
public sealed class DoorClickEvent : EntityEventArgs
{
    public NetEntity Target;

    public DoorClickAction Action;

    public DoorClickEvent(NetEntity target, DoorClickAction action)
    {
        Target = target;
        Action = action;
    }
}
