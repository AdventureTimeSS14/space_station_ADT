namespace Content.Shared.ADT.Sleeping;

[ByRefEvent]
public record struct WakingAttemptEvent(EntityUid? User)
{
    public bool Cancelled = false;
}
