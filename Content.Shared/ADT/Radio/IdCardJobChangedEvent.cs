namespace Content.Shared.ADT.Radio;

[ByRefEvent]
public readonly record struct IdCardJobChangedEvent(EntityUid? PlayerUid);
