namespace Content.Shared.Stealth;

[ByRefEvent]
public record struct CheckStealthWhitelistEvent(EntityUid? User, EntityUid StealthEntity, bool Cancelled = false);
