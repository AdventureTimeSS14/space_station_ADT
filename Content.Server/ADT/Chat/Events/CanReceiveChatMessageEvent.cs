namespace Content.Server.ADT.Chat;

[ByRefEvent]
public record struct CanReceiveChatMessageEvent(EntityUid Source, bool Whisper, bool Cancelled = false);
