namespace Content.Shared.ADT.Sleeping;

[ByRefEvent]
public record struct SleepExamineAttemptEvent(EntityUid User)
{
    public bool Cancelled = false;
}
