using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Language;

[ImplicitDataDefinitionForInheritors]
public partial interface ILanguageType
{
    ProtoId<LanguagePrototype> Language { get; set; }

    /// <summary>
    /// Цвет сообщения в канал "Рядом"
    /// </summary>
    Color? Color { get; set; }

    /// <summary>
    /// Цвет сообщения в канал "Шёпот"
    /// </summary>
    Color? WhisperColor { get; set; }

    /// <summary>
    ///     Спич вербы языка, в обход <see cref="SpeechVerbPrototype"/>
    /// </summary>
    Dictionary<string, List<string>> SuffixSpeechVerbs { get; set; }

    /// <summary>
    ///     Размер шрифта, в обход <see cref="SpeechVerbPrototype"/>
    /// </summary>
    int? FontSize { get; set; }

    /// <summary>
    ///     Прототип шрифта для этого языка
    /// </summary>
    string? Font { get; set; }

    public static List<string> SpeechSuffixes = new()
    {
        { "chat-speech-verb-suffix-exclamation-strong" },
        { "chat-speech-verb-suffix-exclamation" },
        { "chat-speech-verb-suffix-question" },
        { "chat-speech-verb-suffix-stutter" },
        { "chat-speech-verb-suffix-mumble" },
    };

    /// <summary>
    /// Будет ли вызываться <see cref="EntitySpokeEvent"/>
    /// </summary>
    bool RaiseEvent { get; set; }

    void Speak(EntityUid uid, string message, string name, SpeechVerbPrototype verb, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage);

    void Whisper(EntityUid uid, string message, string name, string nameIdentity, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage);
}
