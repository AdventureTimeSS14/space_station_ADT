namespace Content.Shared.ADT.Heretic.Common;

// ADT: перенесено из Content.Goobstation.Common.Speech
// Внимание: эти ивенты должны подниматься чат-системой/барками, см. интеграцию ShadowCloak.

[ByRefEvent]
public record struct GetSpeechSoundEvent(string? SpeechSoundProtoId = null, bool Handled = false);

[ByRefEvent]
public record struct GetEmoteSoundsEvent(string? EmoteSoundProtoId = null, bool Handled = false);

[ByRefEvent]
public record struct GetBarkSourceEntityEvent(EntityUid? Ent = null);
