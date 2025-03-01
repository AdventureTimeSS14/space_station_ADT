using System.Linq;
using Content.Server.ADT.Chat;
using Content.Shared.ADT.Language;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    public Dictionary<ICommonSession, ICChatRecipientData> GetWhisperRecipients(EntityUid source, float clearRange, float muffledRange)
    {
        var recipients = new Dictionary<ICommonSession, ICChatRecipientData>();
        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId)
                continue;

            if (ghostHearing.HasComponent(playerEntity))
            {
                recipients.Add(player, new ICChatRecipientData(-1, true));
                continue;
            }

            var entClearRange = clearRange;
            var entMuffledRange = muffledRange;

            if (TryComp<ChatModifierComponent>(playerEntity, out var modifier))
            {
                if (modifier.Modifiers.ContainsKey(ChatModifierType.WhisperClear))
                    entClearRange = modifier.Modifiers[ChatModifierType.WhisperClear];

                if (modifier.Modifiers.ContainsKey(ChatModifierType.WhisperMuffled))
                    entMuffledRange = modifier.Modifiers[ChatModifierType.WhisperMuffled];
            }


            // even if they are a ghost hearer, in some situations we still need the range
            if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance))
            {
                if (distance < entClearRange)
                {
                    recipients.Add(player, new ICChatRecipientData(distance, false, Muffled: false));
                    continue;
                }
                if (distance < entMuffledRange)
                {
                    recipients.Add(player, new ICChatRecipientData(distance, false, Muffled: true));
                    continue;
                }
            }
        }

        RaiseLocalEvent(new ExpandICChatRecipientsEvent(source, muffledRange, recipients));
        return recipients;
    }

    public void SendWhisper(
                            EntityUid source, ProtoId<LanguagePrototype> language, ChatTransmitRange range,
                            string message, string obfuscatedMessage,
                            string wrappedMessage, string wrappedobfuscatedMessage, string wrappedUnknownMessage,
                            string wrappedLanguageMessage, string wrappedobfuscatedLanguageMessage, string wrappedUnknownLanguageMessage)
    {
        var lang = _prototypeManager.Index(language);

        foreach (var (session, data) in GetWhisperRecipients(source, WhisperClearRange, WhisperMuffledRange))
        {
            EntityUid listener;

            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;
            listener = session.AttachedEntity.Value;

            bool condition = true;
            foreach (var item in lang.Conditions.Where(x => x.RaiseOnListener))
            {
                if (!item.Condition(listener, source, EntityManager))
                    condition = false;
            }
            if (!condition)
                continue;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue; // Won't get logged to chat, and ghosts are too far away to see the pop-up, so we just won't send it to them.

            var (langMessage, wrappedLangMessage, wrappedUnknownLangMessage) =
                    _language.CanUnderstand(listener, language) ?
                    (wrappedMessage, wrappedobfuscatedMessage, wrappedUnknownMessage) :
                    (wrappedLanguageMessage, wrappedobfuscatedLanguageMessage, wrappedUnknownLanguageMessage);

            if (!data.Muffled)
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, message, langMessage, source, false, session.Channel);

            //If listener is too far, they only hear fragments of the message
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedLangMessage, source, false, session.Channel);

            //If listener is too far and has no line of sight, they can't identify the whisperer's identity
            else
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, wrappedUnknownLangMessage, source, false, session.Channel);
        }
    }
}
