using System.Linq;
using Content.Shared.ADT.CCVar; // ADT-Tweak
using Content.Shared.ADT.Language; // ADT-Tweak
using Content.Server.ADT.Chat; // ADT-Tweak
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private void SendEntitySpeak(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        LanguagePrototype? language = null // ADT-Tweak
        )
    {
        // ADT-Tweak-Start
        // if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
        //     return;

        // var message = TransformSpeech(source, originalMessage);
        var message = originalMessage;
        // ADT-Tweak-End

        if (message.Length == 0)
            return;

        // ADT-Tweak-Start
        language ??= _language.GetCurrentLanguage(source);

        if (!ignoreActionBlocker)
        {
            foreach (var item in language.Conditions.Where(x => !x.RaiseOnListener))
            {
                if (!item.Condition(source, null, EntityManager))
                    return;
            }
        }
        // ADT-Tweak-End

        var speech = GetSpeechVerb(source, message);

        // get the entity's apparent name (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            // Check for a speech verb override
            if (nameEv.SpeechVerb != null && ProtoMan.Resolve(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        // ADT-Tweak-Start
        var sanitizedMessage = SanitizeInGameICMessage(source, FormattedMessage.EscapeText(message), out _);
        language.LanguageType.Speak(source, sanitizedMessage, name, speech, range, EntityManager, out var success, out var resultMessage);
        if (!success)
            return;

        if (language.LanguageType.RaiseEvent)
        {
            var ev = new EntitySpokeEvent(source, resultMessage, message, language, null, null);
            RaiseLocalEvent(source, ev, true);
        }
        // ADT-Tweak-End

        // To avoid logging any messages sent by entities that are not players, like vendors, cloning, etc.
        // Also doesn't log if hideLog is true.
        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} as {name}: {originalMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source}: {originalMessage}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} as {name}, original: {originalMessage}, transformed: {message}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source}, original: {originalMessage}, transformed: {message}.");
        }
    }

    private void SendEntityWhisper(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        LanguagePrototype? language = null // ADT-Tweak
        )
    {
        // ADT-Tweak-Start
        // if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
        //     return;

        // var message = TransformSpeech(source, FormattedMessage.RemoveMarkupOrThrow(originalMessage));
        var message = FormattedMessage.RemoveMarkupOrThrow(originalMessage);
        // ADT-Tweak-End
        if (message.Length == 0)
            return;

        // ADT-Tweak-Start
        language ??= _language.GetCurrentLanguage(source);

        if (!ignoreActionBlocker)
        {
            foreach (var item in language.Conditions.Where(x => !x.RaiseOnListener))
            {
                if (!item.Condition(source, null, EntityManager))
                    return;
            }
        }
        // ADT-Tweak-End

        // get the entity's name by visual identity (if no override provided).
        string nameIdentity = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));
        // get the entity's name by voice (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }
        // ADT-Tweak-Start
        name = FormattedMessage.EscapeText(name);
        var sanitizedMessage = SanitizeInGameICMessage(source, FormattedMessage.EscapeText(message), out _);
        language.LanguageType.Whisper(source, sanitizedMessage, name, nameIdentity, range, EntityManager, out var success, out var resultMessage, out var resultObfMessage);
        if (!success)
            return;

        if (language.LanguageType.RaiseEvent)
        {
            var ev = new EntitySpokeEvent(source, resultMessage, message, language, channel, resultObfMessage);
            RaiseLocalEvent(source, ev, true);
        }
        // ADT-Tweak-End

        if (!hideLog)
            if (originalMessage == message)
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source} as {name}: {originalMessage}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source}: {originalMessage}.");
            }
            else
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source} as {name}, original: {originalMessage}, transformed: {message}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source}, original: {originalMessage}, transformed: {message}.");
            }
    }

    protected override void SendEntityEmote(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool checkEmote = true,
        bool ignoreActionBlocker = false,
        NetUserId? author = null
        )
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        string name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", FormattedMessage.RemoveMarkupOrThrow(action)));

        // ADT-Tweak-Start
        if (checkEmote)
            TryEmoteChatInput(source, action);
        SendInVoiceRange(ChatChannel.Emotes, action, wrappedMessage, wrappedMessage, source, range, author, ignoreLanguage: true);
        // ADT-Tweak-End
        if (!hideLog)
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source} as {name}: {action}");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source}: {action}");
    }

    // ReSharper disable once InconsistentNaming
    private void SendLOOC(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (_adminManager.IsAdmin(player))
        {
            if (!_adminLoocEnabled) return;
        }
        else if (!_loocEnabled) return;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-looc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        SendInVoiceRange(ChatChannel.LOOC, message, wrappedMessage, wrappedMessage, source, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, player.UserId, ignoreLanguage: true, checkVisibility: false); // ADT-Tweak
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {source}: {message}");
    }

    private void SendDeadChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        if (!_adminManager.IsAdmin(player) && !_deadChatEnabled)
            return;

        var clients = GetDeadChatClients();
        var playerName = Name(source);
        string wrappedMessage;
        if (_adminManager.IsAdmin(player))
        {
            wrappedMessage = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                ("userName", player.Channel.UserName),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {source}: {message}");
        }
        else
        {
            wrappedMessage = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                ("playerName", (playerName)),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {source}: {message}");
        }

        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, wrappedMessage, source, hideChat, true, clients.ToList(), author: player.UserId);

        // ADT-Tweak-Start
        if (_configurationManager.GetCVar(ADTCCVars.ChatFilterAdminAlertEnable))
        {
            var words = message.Split(
                new[] { ' ', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' },
                StringSplitOptions.RemoveEmptyEntries
            );
            foreach (var word in words)
            {
                if (ChatFilterConstants.OffensiveWords.Contains(word))
                {
                    _chatManager.SendAdminAlert(
                        $"Внимние!! Сущность {ToPrettyString(source)} использовала слово `{word}`" +
                        $" в сообщении, требуется проверка администратора: {message}"
                    );
                    break;
                }
            }
        }
        // ADT-Tweak-End
    }
}
