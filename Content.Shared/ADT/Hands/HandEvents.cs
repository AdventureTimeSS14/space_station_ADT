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
