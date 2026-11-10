using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Cooking;

[Serializable, NetSerializable]
public enum ADTStoveUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTStoveSetTimerMessage : BoundUserInterfaceMessage
{
    public readonly int Seconds;

    public ADTStoveSetTimerMessage(int seconds)
    {
        Seconds = seconds;
    }
}

[Serializable, NetSerializable]
public sealed class ADTStoveAltClickEvent : EntityEventArgs
{
    public readonly NetEntity Stove;
    public readonly NetCoordinates Click;

    public ADTStoveAltClickEvent(NetEntity stove, NetCoordinates click)
    {
        Stove = stove;
        Click = click;
    }
}

[Serializable, NetSerializable]
public sealed class ADTCookwareFlippedEvent : EntityEventArgs
{
    public readonly NetEntity Cookware;

    public ADTCookwareFlippedEvent(NetEntity cookware)
    {
        Cookware = cookware;
    }
}
