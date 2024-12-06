namespace Content.Server.ADT.Eye.Blinding;

[ByRefEvent]
public record struct FlashedEvent(EntityUid? User, EntityUid? Used);
