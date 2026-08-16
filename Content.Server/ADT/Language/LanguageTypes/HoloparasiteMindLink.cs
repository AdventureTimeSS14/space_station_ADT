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
        var admin = IoCManager.Resolve<IAdminManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var chatMan = IoCManager.Resolve<IChatManager>();

        success = false;
        chat.TryProcessRadioMessage(uid, message, out message, out _);
        resultMessage = message;

        if (string.IsNullOrEmpty(message))
            return;

        if (!entMan.TryGetComponent<HoloparasiteMindLinkComponent>(uid, out var link) || link.Partner == null)
            return;

        var clients = Filter.Empty();
        var admins = Filter.Empty();

        var mindQuery = entMan.EntityQueryEnumerator<LanguageSpeakerComponent, ActorComponent>();
        while (mindQuery.MoveNext(out var player, out _, out var actorComp))
        {
            if (player == uid || player == link.Partner)
                clients.AddPlayer(actorComp.PlayerSession);
            else if (admin.IsAdmin(actorComp.PlayerSession))
                admins.AddPlayer(actorComp.PlayerSession);
        }

        string messageWrap;
        string adminMessageWrap;
        var language = proto.Index(Language);

        var wrapKey = ShowName
            ? "chat-manager-send-holoparasite-mind-link-chat-wrap-message-name"
            : "chat-manager-send-holoparasite-mind-link-chat-wrap-message";

        messageWrap = Loc.GetString(wrapKey,
            ("fontType", Font ?? "NotoSansDisplay"),
            ("fontSize", FontSize ?? 12),
            ("defaultFont", "NotoSansDisplay"),
            ("defaultSize", 12),
            ("source", uid),
            ("message", message),
            ("channel", language.LocalizedName));

        adminMessageWrap = Loc.GetString("chat-manager-send-holoparasite-mind-link-chat-wrap-message-admin",
            ("fontType", Font ?? "NotoSansDisplay"),
            ("fontSize", FontSize ?? 12),
            ("defaultFont", "NotoSansDisplay"),
            ("defaultSize", 12),
            ("source", uid),
            ("message", message),
            ("channel", language.LocalizedName));

        if (Color != null)
        {
            messageWrap = $"[color={Color.Value.ToHex()}]{messageWrap}[/color]";
            adminMessageWrap = $"[color={Color.Value.ToHex()}]{adminMessageWrap}[/color]";
        }

        chatMan.ChatMessageToManyFiltered(clients, ChatChannel.CollectiveMind, message, messageWrap, uid, false, false, Color);
        chatMan.ChatMessageToManyFiltered(admins, ChatChannel.CollectiveMind, message, adminMessageWrap, uid, false, false, Color);

        success = true;
    }

    public void Whisper(EntityUid uid, string message, string name, string nameIdentity, ChatTransmitRange range, IEntityManager entMan, out bool success, out string resultMessage, out string resultObfMessage)
    {
        var chat = entMan.System<ChatSystem>();
        var admin = IoCManager.Resolve<AdminManager>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var chatMan = IoCManager.Resolve<IChatManager>();

        success = false;
        chat.TryProcessRadioMessage(uid, message, out message, out _);
        resultMessage = message;
        resultObfMessage = message;

        if (string.IsNullOrEmpty(message))
            return;

        if (!entMan.TryGetComponent<HoloparasiteMindLinkComponent>(uid, out var link) || link.Partner == null)
            return;

        var clients = Filter.Empty();
        var admins = Filter.Empty();

        var mindQuery = entMan.EntityQueryEnumerator<LanguageSpeakerComponent, ActorComponent>();
        while (mindQuery.MoveNext(out var player, out _, out var actorComp))
        {
            if (player == uid || player == link.Partner)
                clients.AddPlayer(actorComp.PlayerSession);
            else if (admin.IsAdmin(actorComp.PlayerSession))
                admins.AddPlayer(actorComp.PlayerSession);
        }

        string messageWrap;
        string adminMessageWrap;
        var language = proto.Index(Language);

        var wrapKey = ShowName
            ? "chat-manager-send-holoparasite-mind-link-chat-wrap-message-name"
            : "chat-manager-send-holoparasite-mind-link-chat-wrap-message";

        messageWrap = Loc.GetString(wrapKey,
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("source", uid),
            ("message", message),
            ("channel", language.LocalizedName));

        adminMessageWrap = Loc.GetString("chat-manager-send-holoparasite-mind-link-chat-wrap-message-admin",
            ("fontType", Font ?? "NotoSansDisplayItalic"),
            ("fontSize", FontSize ?? 11),
            ("defaultFont", "NotoSansDisplayItalic"),
            ("defaultSize", 11),
            ("source", uid),
            ("message", message),
            ("channel", language.LocalizedName));

        if (WhisperColor != null)
        {
            messageWrap = $"[color={WhisperColor.Value.ToHex()}]{messageWrap}[/color]";
            adminMessageWrap = $"[color={WhisperColor.Value.ToHex()}]{adminMessageWrap}[/color]";
        }

        chatMan.ChatMessageToManyFiltered(clients, ChatChannel.CollectiveMind, message, messageWrap, uid, false, false, WhisperColor);
        chatMan.ChatMessageToManyFiltered(admins, ChatChannel.CollectiveMind, message, adminMessageWrap, uid, false, false, WhisperColor);

        success = true;
    }
}
