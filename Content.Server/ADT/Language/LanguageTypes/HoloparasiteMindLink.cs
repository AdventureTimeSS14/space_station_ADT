using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Holoparasite;
using Content.Shared.ADT.Language;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Language;

/// <summary>
/// Ментальная связь голопаразита и носителя: сообщение уходит только паре.
/// </summary>
[DataDefinition]
public sealed partial class HoloparasiteMindLink : ILanguageType
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

        if (!entMan.TryGetComponent<HoloparasiteMindLinkComponent>(uid, out var link) || link.Partner == null)
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

        if (!entMan.TryGetComponent<HoloparasiteMindLinkComponent>(uid, out var link) || link.Partner == null)
            return;

        SendToPair(entMan, uid, link.Partner.Value, message,
            Font ?? "NotoSansDisplayItalic", FontSize ?? 11, "NotoSansDisplayItalic", 11, WhisperColor);

        success = true;
    }

    private void SendToPair(IEntityManager entMan, EntityUid uid, EntityUid partner, string message, string fontType, int fontSize, string defaultFont, int defaultSize, Color? color)
    {
        var admin = IoCManager.Resolve<IAdminManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var chatMan = IoCManager.Resolve<IChatManager>();

        var clients = Filter.Entities(uid, partner);
        var admins = Filter.Empty();

        var playerQuery = entMan.EntityQueryEnumerator<ActorComponent>();
        while (playerQuery.MoveNext(out var player, out var actorComp))
        {
            if (player == uid || player == partner)
                continue;

            if (admin.IsAdmin(actorComp.PlayerSession))
                admins.AddPlayer(actorComp.PlayerSession);
        }

        var language = proto.Index(Language);

        var wrapKey = ShowName
            ? "chat-manager-send-holoparasite-mind-link-chat-wrap-message-name"
            : "chat-manager-send-holoparasite-mind-link-chat-wrap-message";

        var messageWrap = Loc.GetString(wrapKey,
            ("fontType", fontType),
            ("fontSize", fontSize),
            ("defaultFont", defaultFont),
            ("defaultSize", defaultSize),
            ("source", uid),
            ("message", message),
            ("channel", language.LocalizedName));

        if (color != null)
            messageWrap = $"[color={color.Value.ToHex()}]{messageWrap}[/color]";

        chatMan.ChatMessageToManyFiltered(clients, ChatChannel.CollectiveMind, message, messageWrap, uid, false, false, color);
        chatMan.ChatMessageToManyFiltered(admins, ChatChannel.CollectiveMind, message, messageWrap, uid, false, false, color);
    }
}
