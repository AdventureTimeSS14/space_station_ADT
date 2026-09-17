namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Common.Speech
// raised by chat/barks, see ShadowCloak integration

[ByRefEvent]
public record struct GetSpeechSoundEvent(string? SpeechSoundProtoId = null, bool Handled = false);

[ByRefEvent]
public record struct GetEmoteSoundsEvent(string? EmoteSoundProtoId = null, bool Handled = false);

[ByRefEvent]
public record struct GetBarkSourceEntityEvent(EntityUid? Ent = null);
