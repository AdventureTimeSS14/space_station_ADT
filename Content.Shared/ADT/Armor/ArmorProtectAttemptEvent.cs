namespace Content.Shared.ADT.Armor;

[ByRefEvent]
public record struct ArmorProtectAttemptEvent(EntityUid Wearer, bool Cancelled = false);
