using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Chat;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Chat.Systems;

public sealed class ADTDoEmoteSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public void TrySendDoEmote(EntityUid source, string message, ICommonSession player)
    {
        if (!_actionBlocker.CanEmote(source))
            return;

        var text = SharedADTDoEmote.Format(message);
        if (string.IsNullOrEmpty(text))
            return;

        if (_chatManager.MessageCharacterLimit(player, text))
            return;

        if (_chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        var wrapped = Loc.GetString("adt-do-emote-wrap-message",
            ("color", SharedADTDoEmote.ColorHex),
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(text)));

        var plain = Loc.GetString("adt-do-emote-plain-message",
            ("entityName", name),
            ("message", text));

        _chat.SendInVoiceRange(ChatChannel.Emotes,
            plain,
            wrapped,
            wrapped,
            source,
            ChatTransmitRange.Normal,
            player.UserId,
            ignoreLanguage: true);
    }
}
