namespace Content.Shared.Heretic;

// ADT: события урезаны против Goob — желудочный оверрайд и группы метаболизма щитмеда не переносим

[ByRefEvent]
public record struct ImmuneToPoisonDamageEvent(bool Immune = false);
