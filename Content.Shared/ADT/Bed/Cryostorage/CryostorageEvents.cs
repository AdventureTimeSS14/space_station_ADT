namespace Content.Shared.ADT.Bed.Cryostorage;

[ByRefEvent]
public readonly record struct EntityEnteredCryostorageEvent(EntityUid Cryostorage);
