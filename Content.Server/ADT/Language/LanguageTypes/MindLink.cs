using Content.Server.Chat.Systems;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.MindLink;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Language;

/// <summary>
/// Ментальная связь пары сущностей: сообщение уходит только паре.
/// </summary>
[DataDefinition]
public sealed partial class MindLink : ILanguageType
{
    public ProtoId<LanguagePrototype> Language { get; set; }

    [DataField]
    public Color? Color { get; set; }

    [DataField]
    public Color? WhisperColor { get; set; }

    [DataField]
    public bool RaiseEvent { get; set; } = false;

    [DataField]
    public bool ShowName = true;

    [DataField("verbs")]
    public Dictionary<string, List<string>> SuffixSpeechVerbs { get; set; } = new()
    {
        { "chat-speech-verb-suffix-exclamation-strong", new() },
        { "chat-speech-verb-suffix-exclamation", new() },
        { "chat-speech-verb-suffix-question", new() },
        { "chat-speech-verb-suffix-stutter", new() },
        { "chat-speech-verb-suffix-mumble", new() },
    };

    [DataField]
    public int? FontSize { get; set; } = null;

    [DataField]
    public string? Font { get; set; } = null;

    public void Speak(EntityUid uid, string message, string name, SpeechVerbPrototype verb, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage)
    {
        var chat = entMan.System<ChatSystem>();

        success = false;
        chat.TryProcessRadioMessage(uid, message, out message, out _);
        resultMessage = message;

        if (string.IsNullOrEmpty(message))
            return;

        if (!entMan.TryGetComponent<MindLinkComponent>(uid, out var link) || link.Partner == null)
            return;

        SendToPair(entMan, uid, link.Partner.Value, message,
            Font ?? "NotoSansDisplay", FontSize ?? 12, "NotoSansDisplay", 12, Color);

        success = true;
    }

    public void Whisper(EntityUid uid, string message, string name, string nameIdentity, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage)
    {
        var chat = entMan.System<ChatSystem>();

        success = false;
        chat.TryProcessRadioMessage(uid, message, out message, out _);
        resultMessage = message;
        resultObfMessage = message;

        if (string.IsNullOrEmpty(message))
            return;

        if (!entMan.TryGetComponent<MindLinkComponent>(uid, out var link) || link.Partner == null)
            return;

        SendToPair(entMan, uid, link.Partner.Value, message,
            Font ?? "NotoSansDisplayItalic", FontSize ?? 11, "NotoSansDisplayItalic", 11, WhisperColor);

        success = true;
    }

    private void SendToPair(IEntityManager entMan, EntityUid uid, EntityUid partner, string message, string fontType, int fontSize, string defaultFont, int defaultSize, Color? color)
    {
        var language = entMan.System<LanguageSystem>();
        var proto = IoCManager.Resolve<IPrototypeManager>();

        var wrapKey = ShowName
            ? "chat-manager-send-mind-link-chat-wrap-message-name"
            : "chat-manager-send-mind-link-chat-wrap-message";

        var messageWrap = Loc.GetString(wrapKey,
            ("fontType", fontType),
            ("fontSize", fontSize),
            ("defaultFont", defaultFont),
            ("defaultSize", defaultSize),
            ("source", uid),
            ("message", message),
            ("channel", proto.Index(Language).LocalizedName));

        if (color != null)
            messageWrap = $"[color={color.Value.ToHex()}]{messageWrap}[/color]";

        var adminWrapKey = ShowName
            ? "chat-manager-send-mind-link-chat-wrap-message-admin"
            : "chat-manager-send-mind-link-chat-wrap-message";

        var adminMessageWrap = Loc.GetString(adminWrapKey,
            ("fontType", fontType),
            ("fontSize", fontSize),
            ("defaultFont", defaultFont),
            ("defaultSize", defaultSize),
            ("source", uid),
            ("message", message),
            ("channel", proto.Index(Language).LocalizedName));

        if (color != null)
            adminMessageWrap = $"[color={color.Value.ToHex()}]{adminMessageWrap}[/color]";

        language.SendChannelMessageToPair(uid, partner, message, messageWrap, adminMessageWrap, ChatChannel.CollectiveMind, color);
    }
}
