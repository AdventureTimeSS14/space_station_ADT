using System.Linq;
using Content.Server.ADT.Chat;
using Content.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private Dictionary<ICommonSession, ICChatRecipientData> GetWhisperRecipients(EntityUid source, float clearRange, float muffledRange)
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

            var observer = ghostHearing.HasComponent(playerEntity);
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
                    recipients.Add(player, new ICChatRecipientData(distance, observer, Muffled: false));
                    continue;
                }
                else if (distance < entMuffledRange)
                {
                    recipients.Add(player, new ICChatRecipientData(distance, observer, Muffled: true));
                    continue;
                }
            }

            if (observer)
                recipients.Add(player, new ICChatRecipientData(-1, true));
        }

        RaiseLocalEvent(new ExpandICChatRecipientsEvent(source, muffledRange, recipients));
        return recipients;
    }
}
