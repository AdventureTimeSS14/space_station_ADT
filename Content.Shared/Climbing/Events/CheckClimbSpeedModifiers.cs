namespace Content.Shared.Climbing.Events;   // ADT File

[ByRefEvent]
public record struct CheckClimbSpeedModifiersEvent(EntityUid User, EntityUid Climber, EntityUid Climbable, float Time)
{
}
