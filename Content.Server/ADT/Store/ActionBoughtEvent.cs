namespace Content.Server.ADT.Store;

[ByRefEvent]
public record struct ActionBoughtEvent(EntityUid? ActionEntity);
