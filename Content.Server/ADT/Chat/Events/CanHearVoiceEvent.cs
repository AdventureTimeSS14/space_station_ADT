namespace Content.Server.ADT.Chat;

[ByRefEvent]
public record struct CanHearVoiceEvent(EntityUid Source, bool Whisper, bool Cancelled = false);
