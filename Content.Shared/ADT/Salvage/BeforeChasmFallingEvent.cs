namespace Content.Shared.ADT.Salvage.Components;

[ByRefEvent]
public record struct BeforeChasmFallingEvent(EntityUid Entity, bool Cancelled = false);
