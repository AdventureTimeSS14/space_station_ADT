using Content.Shared.ADT.Language;

namespace Content.Server.ADT.TTS;

[ByRefEvent]
public readonly record struct ADTTunableRadioSpokeEvent(
    EntityUid Source,
    string Message,
    List<EntityUid> Speakers,
    LanguagePrototype Language,
    string? Effect,
    bool IsWhisper);
