namespace Content.Shared.ADT.Heretic.Common;

// ADT: перенесено из Content.Goobstation.Common.Weapons (GetLightAttackRangeEvent.cs)
// Эти ивенты должна поднимать милишная система при лёгкой атаке; см. интеграцию клинков еретика.

[ByRefEvent]
public record struct GetLightAttackRangeEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);

[ByRefEvent]
public record struct LightAttackSpecialInteractionEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);
