namespace Content.Shared.ADT.Sandevistan;

/// <summary>
/// Raised on the user when a DoAfter is about to start. Allows systems to reduce the delay.
/// </summary>
[ByRefEvent]
public record struct ModifyDoAfterDelayEvent(TimeSpan Delay)
{
    public TimeSpan Delay = Delay;
}
