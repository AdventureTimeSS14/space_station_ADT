using Robust.Shared.Map;

namespace Content.Shared.ADT.Hands;

public sealed class BeforeVirtualItemDeletedEvent : CancellableEntityEventArgs
{
    public EntityUid BlockingEntity;
    public EntityUid User;
    public BeforeVirtualItemDeletedEvent(EntityUid blockingEntity, EntityUid user)
    {
        BlockingEntity = blockingEntity;
        User = user;
    }
}

public sealed class BeforeVirtualItemThrownEvent : CancellableEntityEventArgs
{
    public EntityUid BlockingEntity;
    public EntityUid User;
    public EntityCoordinates Coords;
    public BeforeVirtualItemThrownEvent(EntityUid blockingEntity, EntityUid user, EntityCoordinates coords)
    {
        BlockingEntity = blockingEntity;
        User = user;
        Coords = coords;
    }
}
