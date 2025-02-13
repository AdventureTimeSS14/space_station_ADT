namespace Content.Shared.ADT.Damage.Events;

[ByRefEvent]
public record struct BeforeStaminaCritEvent(bool Cancelled = false);
