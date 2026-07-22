namespace Content.Shared.ADT.Mobs;

[ByRefEvent]
public record struct ShouldLogMobStateChangeEvent(EntityUid Target, EntityUid? Origin)
{
    public bool Cancelled;
}
