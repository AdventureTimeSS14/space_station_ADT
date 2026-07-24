namespace Content.Shared.ADT.Heretic.Common;

// ADT: перенесено из Content.Goobstation.Common.Conversion
// Поднимайте этот ивент перед конверсией (революционеры и т.п.), еретики его блокируют.

[ByRefEvent]
public record struct BeforeConversionEvent(EntityUid Uid, bool Blocked = false);
