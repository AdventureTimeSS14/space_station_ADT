using Content.Server.Chat.Systems;
using Content.Shared.ADT.Language;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Language;

[DataDefinition]
public sealed partial class Generic : ILanguageType
{
    public ProtoId<LanguagePrototype> Language { get; set; }

    [DataField]
    public Color? Color { get; set; }

    [DataField]
    public Color? WhisperColor { get; set; }

    /// <inheritdoc/>
    [DataField]
    public bool RaiseEvent { get; set; } = true;

    /// <summary>
    /// Слоги/фразы, из которых составляется "неизвестное" сообщение
    /// </summary>
    [DataField(required: true)]
    public List<string> Replacement = new();

    /// <summary>
    /// От значения данного поля зависит то, будет язык заменять слоги, или фразы целиком
    /// true - слоги, false - фразы
    /// </summary>
    [DataField("obfuscateSyllables")]
    public bool ObfuscateSyllables { get; private set; } = false;

    /// <inheritdoc/>
    [DataField("verbs")]
    public Dictionary<string, List<string>> SuffixSpeechVerbs { get; set; } = new()
    {
        { "chat-speech-verb-suffix-exclamation-strong", new() },
        { "chat-speech-verb-suffix-exclamation", new() },
        { "chat-speech-verb-suffix-question", new() },
        { "chat-speech-verb-suffix-stutter", new() },
        { "chat-speech-verb-suffix-mumble", new() },
        { "Default", new() },
    };

    /// <inheritdoc/>
    [DataField]
    public int? FontSize { get; set; } = null;

    /// <inheritdoc/>
    [DataField]
    public string? Font { get; set; } = null;



    public void Speak(EntityUid uid, string message, string name, SpeechVerbPrototype verb, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage)
    {
        var lang = entMan.System<LanguageSystem>();
        var chat = entMan.System<ChatSystem>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        success = false;

        message = chat.TransformSpeech(uid, message);

        string coloredMessage = lang.AccentuateMessage(uid, Language, message);
        string coloredLanguageMessage = lang.ObfuscateMessage(uid, message, Replacement, ObfuscateSyllables);
        resultMessage = FormattedMessage.EscapeText(coloredMessage);
        if (string.IsNullOrEmpty(coloredMessage))
            return;

        // Apply language color
        if (Color != null)
        {
            coloredMessage = $"[color={Color.Value.ToHex()}]{coloredMessage}[/color]";
            coloredLanguageMessage = $"[color={Color.Value.ToHex()}]{coloredLanguageMessage}[/color]";
        }

        // Getting verbs
        List<string> verbStrings = verb.SpeechVerbStrings;
        bool verbsReplaced = false;
        foreach (var str in ILanguageType.SpeechSuffixes)
        {
            if (message.EndsWith(Loc.GetString(str)) && SuffixSpeechVerbs.TryGetValue(str, out var strings) && strings.Count > 0)
            {
                verbStrings = strings;
                verbsReplaced = true;
            }
        }

        if (!verbsReplaced && SuffixSpeechVerbs.TryGetValue("Default", out var defaultStrings) && defaultStrings.Count > 0)
            verbStrings = defaultStrings;

        int fontSize = FontSize.HasValue ? FontSize.Value : verb.FontSize;
        string font = Font != null && Font != "" ? Font : verb.FontId;

        name = FormattedMessage.EscapeText(name);

        // Build messages
        var wrappedMessage = Loc.GetString(verb.Bold && Font == null ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString(random.Pick(verbStrings))),
            ("fontType", font),
            ("fontSize", fontSize),
            ("defaultFont", verb.FontId),
            ("defaultSize", verb.FontSize),
            ("message", coloredMessage));

        var wrappedLanguageMessage = Loc.GetString(verb.Bold && Font == null ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString(random.Pick(verbStrings))),
            ("fontType", font),
            ("fontSize", fontSize),
            ("defaultFont", verb.FontId),
            ("defaultSize", verb.FontSize),
            ("message", coloredLanguageMessage));

        // Send
        chat.SendInVoiceRange(ChatChannel.Local, message, wrappedMessage, wrappedLanguageMessage, uid, range, language: Language);
        success = true;
    }

    public void Whisper(EntityUid uid, string message, string name, string nameIdentity, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage)
    {
        var lang = entMan.System<LanguageSystem>();
        var chat = entMan.System<ChatSystem>();
        success = false;

        message = chat.TransformSpeech(uid, message);

        var accentMessage = lang.AccentuateMessage(uid, Language, message);
        var languageMessage = lang.ObfuscateMessage(uid, message, Replacement, ObfuscateSyllables);
        var obfuscatedMessage = chat.ObfuscateMessageReadability(accentMessage, 0.2f);
        var obfuscatedLanguageMessage = chat.ObfuscateMessageReadability(languageMessage, 0.2f);
        resultMessage = FormattedMessage.EscapeText(accentMessage);
        resultObfMessage = FormattedMessage.EscapeText(obfuscatedMessage);
        if (string.IsNullOrEmpty(accentMessage))
            return;

        if (WhisperColor != null)
        {
            accentMessage = $"[color={WhisperColor.Value.ToHex()}]{accentMessage}[/color]";
            languageMessage = $"[color={WhisperColor.Value.ToHex()}]{languageMessage}[/color]";
            obfuscatedMessage = $"[color={WhisperColor.Value.ToHex()}]{obfuscatedMessage}[/color]";
            obfuscatedLanguageMessage = $"[color={WhisperColor.Value.ToHex()}]{obfuscatedLanguageMessage}[/color]";
        }

        name = FormattedMessage.EscapeText(name);

        var wrappedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", name),
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("message", accentMessage));

        var wrappedobfuscatedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", nameIdentity),
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("message", obfuscatedMessage));

        var wrappedUnknownMessage = Loc.GetString("chat-manager-entity-whisper-unknown-wrap-message",
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("message", obfuscatedMessage));

        var wrappedLanguageMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("entityName", name), ("message", languageMessage));

        var wrappedobfuscatedLanguageMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("entityName", nameIdentity), ("message", obfuscatedLanguageMessage));

        var wrappedUnknownLanguageMessage = Loc.GetString("chat-manager-entity-whisper-unknown-wrap-message",
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("message", obfuscatedLanguageMessage));

        chat.SendWhisper(uid, Language, range, message, obfuscatedMessage,
                        wrappedMessage, wrappedobfuscatedMessage, wrappedUnknownMessage,
                        wrappedLanguageMessage, wrappedobfuscatedLanguageMessage, wrappedUnknownLanguageMessage);
        success = true;
    }
}
