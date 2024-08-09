namespace Content.Shared.Climbing.Events;

[ByRefEvent]
public record struct CheckClimbSpeedModifiersEvent(EntityUid User, EntityUid Climber, EntityUid Climbable, float Time)
{
}
