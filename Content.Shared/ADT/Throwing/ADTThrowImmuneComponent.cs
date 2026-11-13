namespace Content.Shared.ADT.Throwing;

[RegisterComponent]
public sealed partial class ADTThrowImmuneComponent : Component
{
    [ViewVariables]
    public bool Bypass;
}

[ByRefEvent]
public record struct ADTBeforeThrownEvent(EntityUid? User, bool Cancelled = false);
