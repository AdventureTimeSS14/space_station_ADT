namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob GetLightAttackRangeEvent.cs
// raised by melee system on light attack, see heretic blades

[ByRefEvent]
public record struct GetLightAttackRangeEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);

[ByRefEvent]
public record struct LightAttackSpecialInteractionEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);
