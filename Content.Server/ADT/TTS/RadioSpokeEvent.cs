using Content.Shared.ADT.Language;
using Content.Shared.Radio;

namespace Content.Server.ADT.TTS;

[ByRefEvent]
public readonly record struct RadioSpokeEvent(
    EntityUid Source,
    string Message,
    RadioChannelPrototype Channel,
    List<EntityUid> Receivers,
    LanguagePrototype Language);
