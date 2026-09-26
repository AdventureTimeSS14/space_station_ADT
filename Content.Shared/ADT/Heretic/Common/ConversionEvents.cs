namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Common.Conversion
// raise before conversion (revs etc), heretics block it

[ByRefEvent]
public record struct BeforeConversionEvent(EntityUid Uid, bool Blocked = false);
