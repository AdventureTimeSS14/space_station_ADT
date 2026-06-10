namespace Content.Shared.ADT.Grab;

public sealed class BeforeHarmfulActionEvent(EntityUid user, HarmfulActionType type) : CancellableEntityEventArgs
{
    public EntityUid User = user;
    public HarmfulActionType Type = type;
}

public enum HarmfulActionType
{
    Grab,
}

[ByRefEvent]
public record struct GrabAttemptEvent(
    EntityUid Puller,
    bool IgnoreCombatMode = false,
    GrabStage? GrabStageOverride = null,
    float EscapeAttemptModifier = 1f)
{
    public bool Grabbed = false;
}

[ByRefEvent]
public record struct GrabAttemptReleaseEvent(
    EntityUid? user,
    EntityUid puller)
{
    public bool Released = true;
}

[ByRefEvent]
public record struct CheckGrabbedEvent(bool IsGrabbed = false);

[ByRefEvent]
public record struct RaiseGrabModifierEventEvent(
    EntityUid User,
    int Stage,
    GrabStage? NewStage = null,
    float Multiplier = 1f,
    float Modifier = 0f,
    float SpeedMultiplier = 1f);

[ByRefEvent]
public record struct FindGrabbingItemEvent(EntityUid? Grabbed = null, EntityUid? GrabbingItem = null);

[ByRefEvent]
public readonly record struct StopGrabbingItemPullEvent(EntityUid PulledUid);
