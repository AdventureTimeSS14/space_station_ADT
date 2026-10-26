namespace Content.Shared.Heretic;

// ADT: trimmed vs Goob, no shitmed stomach/metabolism groups

[ByRefEvent]
public record struct ImmuneToPoisonDamageEvent(bool Immune = false);
