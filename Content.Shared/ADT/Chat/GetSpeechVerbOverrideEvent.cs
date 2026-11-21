using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chat;

[ByRefEvent]
public record struct GetSpeechVerbOverrideEvent(string Message, ProtoId<SpeechVerbPrototype>? SpeechVerb = null);
