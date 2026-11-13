namespace Content.Shared.Medical.Stethoscope;

public sealed class ADTStethoscopeListenedEvent(EntityUid user) : EntityEventArgs
{
    public readonly EntityUid User = user;
}
