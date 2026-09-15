using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.Power.Components;
using Content.Server.Radio;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.NanoChat;
using Content.Shared.PDA;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedNanoChatSystem _nanoChat = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    // Messages in notifications get cut off after this point
    // no point in storing it on the comp
    private const int NotificationMaxLength = 64;

    // Group chat limits
    private const int MaxGroupMembers = 30;
    private const int MaxGroupNameLength = 32;
    private const int MaxPendingInvites = 10;
    private const int MaxGroupHistoryCopied = 100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Update card references for any cartridges that need it
        var query = EntityQueryEnumerator<NanoChatCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var nanoChat, out var cartridge))
        {
            if (cartridge.LoaderUid == null)
                continue;

            // Check if we need to update our card reference
            if (!TryComp<PdaComponent>(cartridge.LoaderUid, out var pda))
                continue;

            var newCard = pda.ContainedId;
            var currentCard = nanoChat.Card;

            // If the cards match, nothing to do
            if (newCard == currentCard)
                continue;

            // Update card reference
            nanoChat.Card = newCard;

            // Update UI state since card reference changed
            UpdateUI((uid, nanoChat), cartridge.LoaderUid.Value);
        }
    }

    /// <summary>
    ///     Handles incoming UI messages from the NanoChat cartridge.
    /// </summary>
    private void OnMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not NanoChatUiMessageEvent msg)
            return;

        if (!GetCardEntity(GetEntity(args.LoaderUid), out var card))
            return;

        switch (msg.Type)
        {
            case NanoChatUiMessageType.NewChat:
                HandleNewChat(card, msg);
                break;
            case NanoChatUiMessageType.SelectChat:
                HandleSelectChat(card, msg);
                break;
            case NanoChatUiMessageType.CloseChat:
                HandleCloseChat(card);
                break;
            case NanoChatUiMessageType.ToggleMute:
                HandleToggleMute(card);
                break;
            case NanoChatUiMessageType.DeleteChat:
                HandleDeleteChat(card, msg);
                break;
            case NanoChatUiMessageType.SendMessage:
                HandleSendMessage(ent, card, msg);
                break;
            case NanoChatUiMessageType.ToggleListNumber:
                HandleToggleListNumber(card);
                break;
            case NanoChatUiMessageType.CreateGroup:
                HandleCreateGroup(card, msg);
                break;
            case NanoChatUiMessageType.InviteToGroup:
                HandleInviteToGroup(card, msg);
                break;
            case NanoChatUiMessageType.AcceptInvite:
                HandleAcceptInvite(card, msg);
                break;
            case NanoChatUiMessageType.DeclineInvite:
                HandleDeclineInvite(card, msg);
                break;
            case NanoChatUiMessageType.JoinPublicGroup:
                HandleJoinPublicGroup(card, msg);
                break;
            case NanoChatUiMessageType.LeaveGroup:
                HandleLeaveGroup(card, msg);
                break;
            case NanoChatUiMessageType.KickMember:
                HandleKickMember(card, msg);
                break;
            case NanoChatUiMessageType.DeleteGroup:
                HandleDeleteGroup(card, msg);
                break;
        }

        UpdateUI(ent, GetEntity(args.LoaderUid));
    }

    /// <summary>
    ///     Gets the ID card entity associated with a PDA.
    /// </summary>
    /// <param name="loaderUid">The PDA entity ID</param>
    /// <param name="card">Output parameter containing the found card entity and component</param>
    /// <returns>True if a valid NanoChat card was found</returns>
    private bool GetCardEntity(
        EntityUid loaderUid,
        out Entity<NanoChatCardComponent> card)
    {
        card = default;

        // Get the PDA and check if it has an ID card
        if (!TryComp<PdaComponent>(loaderUid, out var pda) ||
            pda.ContainedId == null ||
            !TryComp<NanoChatCardComponent>(pda.ContainedId, out var idCard))
            return false;

        card = (pda.ContainedId.Value, idCard);
        return true;
    }

    /// <summary>True, если на станции есть карта НаноМакс с указанным номером.</summary>
    private bool CardExistsWithNumber(uint number)
    {
        var query = AllEntityQuery<NanoChatCardComponent>();
        while (query.MoveNext(out _, out var card))
        {
            if (card.Number == number)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Handles creation of a new chat conversation.
    /// </summary>
    private void HandleNewChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || msg.RecipientNumber == card.Comp.Number)
            return;

        // Не создаём чат с номером, которого нет ни на одной карте (иначе сообщения уходят в никуда).
        if (!CardExistsWithNumber(msg.RecipientNumber.Value))
            return;

        var name = msg.Content;
        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
        }

        var jobTitle = msg.RecipientJob;
        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            jobTitle = jobTitle.Trim();
        }

        // Add new recipient
        var recipient = new NanoChatRecipient(msg.RecipientNumber.Value,
            name,
            jobTitle);

        // Initialize or update recipient
        _nanoChat.SetRecipient((card, card.Comp), msg.RecipientNumber.Value, recipient);

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} created new NanoChat conversation with #{msg.RecipientNumber:D4} ({name})");

        var recipientEv = new NanoChatRecipientUpdatedEvent(card);
        RaiseLocalEvent(ref recipientEv);
        UpdateUIForCard(card);
    }

    /// <summary>
    ///     Handles selecting a chat conversation.
    /// </summary>
    private void HandleSelectChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null)
            return;

        _nanoChat.SetCurrentChat((card, card.Comp), msg.RecipientNumber);

        // Clear unread flag when selecting chat
        if (_nanoChat.GetRecipient((card, card.Comp), msg.RecipientNumber.Value) is { } recipient)
        {
            _nanoChat.SetRecipient((card, card.Comp),
                msg.RecipientNumber.Value,
                recipient with { HasUnread = false });
        }

        // Clear unread flag for groups too
        if (_nanoChat.GetGroup((card, card.Comp), msg.RecipientNumber.Value) is { } group)
        {
            _nanoChat.SetGroup((card, card.Comp), group with { HasUnread = false });
        }
    }

    /// <summary>
    ///     Handles closing the current chat conversation.
    /// </summary>
    private void HandleCloseChat(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetCurrentChat((card, card.Comp), null);
    }

    /// <summary>
    ///     Handles deletion of a chat conversation.
    /// </summary>
    private void HandleDeleteChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || card.Comp.Number == null)
            return;

        // Delete chat but keep the messages
        var deleted = _nanoChat.TryDeleteChat((card, card.Comp), msg.RecipientNumber.Value, true);

        if (!deleted)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} deleted NanoChat conversation with #{msg.RecipientNumber:D4}");

        UpdateUIForCard(card);
    }

    /// <summary>
    ///     Handles toggling notification mute state.
    /// </summary>
    private void HandleToggleMute(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetNotificationsMuted((card, card.Comp), !_nanoChat.GetNotificationsMuted((card, card.Comp)));
        UpdateUIForCard(card);
    }

    private void HandleToggleListNumber(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetListNumber((card, card.Comp), !_nanoChat.GetListNumber((card, card.Comp)));
        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles sending a new message in a chat conversation.
    /// </summary>
    private void HandleSendMessage(Entity<NanoChatCartridgeComponent> cartridge,
        Entity<NanoChatCardComponent> card,
        NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || card.Comp.Number == null)
            return;

        // Group chat messages are routed to all group members.
        if (_nanoChat.GetGroup((card, card.Comp), msg.RecipientNumber.Value) != null)
        {
            HandleSendGroupMessage(cartridge, card, msg);
            return;
        }

        if (!EnsureRecipientExists(card, msg.RecipientNumber.Value))
            return;

        var content = msg.Content;
        if (!string.IsNullOrWhiteSpace(content))
        {
            content = FormattedMessage.EscapeText(content.Trim());
            if (content.Length > NanoChatMessage.MaxContentLength)
                content = content[..NanoChatMessage.MaxContentLength];
        }

        // Create and store message for sender
        var message = new NanoChatMessage(
            _timing.CurTime,
            content,
            (uint)card.Comp.Number
        );

        // Attempt delivery
        var (deliveryFailed, recipients) = AttemptMessageDelivery(cartridge, msg.RecipientNumber.Value);

        // Update delivery status
        message = message with { DeliveryFailed = deliveryFailed };

        // Store message in sender's outbox under recipient's number
        _nanoChat.AddMessage((card, card.Comp), msg.RecipientNumber.Value, message);

        // Log message attempt
        var recipientsText = recipients.Count > 0
            ? string.Join(", ", recipients.Select(r => ToPrettyString(r)))
            : $"#{msg.RecipientNumber:D4}";

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(card):user} sent NanoChat message to {recipientsText}: {content}{(deliveryFailed ? " [DELIVERY FAILED]" : "")}");

        var msgEv = new NanoChatMessageReceivedEvent(card);
        RaiseLocalEvent(ref msgEv);

        if (deliveryFailed)
            return;

        foreach (var recipient in recipients)
        {
            DeliverMessageToRecipient(card, recipient, message);
        }
    }

    /// <summary>
    ///     Handles sending a message to a group chat: routes it to every member card.
    /// </summary>
    private void HandleSendGroupMessage(Entity<NanoChatCartridgeComponent> cartridge,
        Entity<NanoChatCardComponent> card,
        NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || msg.Content == null || card.Comp.Number == null)
            return;

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) is not { } group)
            return;

        var content = msg.Content;
        if (!string.IsNullOrWhiteSpace(content))
        {
            content = FormattedMessage.EscapeText(content.Trim());
            if (content.Length > NanoChatMessage.MaxContentLength)
                content = content[..NanoChatMessage.MaxContentLength];
        }

        var message = new NanoChatMessage(
            _timing.CurTime,
            content,
            (uint)card.Comp.Number
        );

        // Deliver to every member except the sender. A group with only the
        // sender is still a valid chat: the message is stored in the history.
        var otherMembers = group.Members.Where(m => m.Number != card.Comp.Number).ToList();
        var deliveredAny = false;
        var deliveredCards = new List<Entity<NanoChatCardComponent>>();
        foreach (var member in otherMembers)
        {
            var (deliveryFailed, recipientCards) = AttemptMessageDelivery(cartridge, member.Number);
            if (deliveryFailed)
                continue;

            deliveredAny = true;
            deliveredCards.AddRange(recipientCards);
        }

        message = message with { DeliveryFailed = otherMembers.Count > 0 && !deliveredAny };

        // Store in sender's outbox under the group number.
        _nanoChat.AddGroupMessage((card, card.Comp), groupNumber, message);

        var deliveredText = deliveredCards.Count > 0
            ? string.Join(", ", deliveredCards.Select(r => ToPrettyString(r)))
            : $"#{groupNumber:D4} ({group.Members.Count} members)";

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(card):user} sent NanoChat group message to #{groupNumber:D4} '{group.Name}' ({deliveredCards.Count}/{group.Members.Count} delivered): {content}{(message.DeliveryFailed ? " [DELIVERY FAILED]" : "")}");

        var msgEv = new NanoChatMessageReceivedEvent(card);
        RaiseLocalEvent(ref msgEv);

        if (message.DeliveryFailed)
            return;

        foreach (var recipient in deliveredCards)
        {
            // Skip cards that left the group meanwhile.
            if (_nanoChat.GetGroup((recipient, recipient.Comp), groupNumber) == null)
                continue;

            _nanoChat.AddGroupMessage((recipient, recipient.Comp), groupNumber, message with { DeliveryFailed = false });
            HandleGroupUnreadNotification(recipient, group, message);

            var recipientEv = new NanoChatMessageReceivedEvent(recipient);
            RaiseLocalEvent(ref recipientEv);
            UpdateUIForCard(recipient);
        }
    }

    /// <summary>
    ///     Handles unread status and notifications for a group message.
    /// </summary>
    private void HandleGroupUnreadNotification(Entity<NanoChatCardComponent> recipient,
        NanoChatGroup group,
        NanoChatMessage message)
    {
        if (recipient.Comp.Number == null)
            return;

        var hasSelectedCurrentChat = _nanoChat.GetCurrentChat((recipient, recipient.Comp)) == group.Number;

        if (!hasSelectedCurrentChat)
            _nanoChat.SetGroup((recipient, recipient.Comp), group with { HasUnread = true });

        if (recipient.Comp.NotificationsMuted ||
            recipient.Comp.PdaUid is not {} pdaUid ||
            !TryComp<CartridgeLoaderComponent>(pdaUid, out var loader) ||
            // Don't notify if the recipient has the NanoChat program open with this chat selected.
            (hasSelectedCurrentChat &&
                _ui.IsUiOpen(pdaUid, PdaUiKey.Key) &&
                HasComp<NanoChatCartridgeComponent>(loader.ActiveProgram)))
            return;

        _cartridge.SendNotification(pdaUid,
            Loc.GetString("nano-chat-group-message-title", ("group", group.Name)),
            Loc.GetString("nano-chat-new-message-body", ("message", TruncateMessage(message.Content))),
            loader);
    }

    /// <summary>
    ///     Finds the card with the given NanoChat number.
    /// </summary>
    private Entity<NanoChatCardComponent>? GetCardByNumber(uint number)
    {
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Number == number)
                return (uid, card);
        }

        return null;
    }

    /// <summary>
    ///     Finds a card that has the given group and returns the group definition.
    /// </summary>
    private NanoChatGroup? FindGroupDefinition(uint groupNumber)
    {
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (_nanoChat.GetGroup((uid, card), groupNumber) is { } group)
                return group;
        }

        return null;
    }

    /// <summary>
    ///     Finds all cards that are members of the given group.
    /// </summary>
    private List<Entity<NanoChatCardComponent>> GetGroupMemberCards(uint groupNumber)
    {
        var cards = new List<Entity<NanoChatCardComponent>>();
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (_nanoChat.GetGroup((uid, card), groupNumber) != null)
                cards.Add((uid, card));
        }

        return cards;
    }

    /// <summary>
    ///     Rewrites the group definition on all member cards.
    /// </summary>
    private void WriteGroupToAllMembers(NanoChatGroup group)
    {
        foreach (var memberCard in GetGroupMemberCards(group.Number))
        {
            _nanoChat.SetGroup((memberCard, memberCard.Comp), group);
        }
    }

    /// <summary>
    ///     Handles creation of a new group chat.
    /// </summary>
    private void HandleCreateGroup(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.Content == null || msg.Members == null || card.Comp.Number == null)
            return;

        var name = msg.Content.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (name.Length > MaxGroupNameLength)
            name = name[..MaxGroupNameLength];

        // Max 29 other members + the owner = MaxGroupMembers.
        if (msg.Members.Count > MaxGroupMembers - 1)
            return;

        var memberNumbers = new List<uint>();
        foreach (var memberNumber in msg.Members.Distinct())
        {
            if (memberNumber == card.Comp.Number)
                continue;

            // Only real cards can be added.
            if (GetCardByNumber(memberNumber) == null)
                continue;

            memberNumbers.Add(memberNumber);
        }

        // Generate a unique group number.
        var groupNumber = GenerateGroupNumber();

        var members = new List<NanoChatMember> { GetMemberInfo(card.Comp.Number.Value) ?? new NanoChatMember(card.Comp.Number.Value, "Unknown") };
        foreach (var memberNumber in memberNumbers)
        {
            members.Add(GetMemberInfo(memberNumber) ?? new NanoChatMember(memberNumber, "Unknown"));
        }

        var group = new NanoChatGroup(groupNumber, name, msg.IsPublic, card.Comp.Number.Value, members);

        // Add to the owner's card.
        _nanoChat.SetGroup((card, card.Comp), group);

        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(msg.Actor):user} created NanoChat group #{groupNumber:D4} '{name}' ({(msg.IsPublic ? "public" : "closed")}) with {members.Count} members: {string.Join(", ", members.Select(m => $"#{m.Number:D4} ({m.Name})"))}");

        // Add to every initial member's card and notify them.
        foreach (var memberCard in memberNumbers.Select(GetCardByNumber))
        {
            if (memberCard == null)
                continue;

            _nanoChat.SetGroup((memberCard.Value, memberCard.Value.Comp), group);
            SendGroupNotification(memberCard.Value, "nano-chat-group-added-title", ("group", name));
            UpdateUIForCard(memberCard.Value);
        }

        UpdateUIForCard(card);

        // Public groups must show up in everyone's chat list right away.
        if (msg.IsPublic)
            UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles inviting a player to a group.
    /// </summary>
    private void HandleInviteToGroup(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || msg.TargetNumber is not { } targetNumber || card.Comp.Number == null)
            return;

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) is not { } group)
            return;

        if (group.Members.Any(m => m.Number == targetNumber))
            return;

        if (group.Members.Count >= MaxGroupMembers)
            return;

        if (GetCardByNumber(targetNumber) is not { } targetCard)
            return;

        if (targetCard.Comp.Invites.Count >= MaxPendingInvites)
            return;

        if (_nanoChat.GetInvite((targetCard, targetCard.Comp), groupNumber) != null)
            return;

        var senderName = group.Members.FirstOrDefault(m => m.Number == card.Comp.Number).Name;

        _nanoChat.AddInvite((targetCard, targetCard.Comp),
            new NanoChatGroupInvite(groupNumber, group.Name, card.Comp.Number.Value, senderName));

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} invited #{targetNumber:D4} to NanoChat group #{groupNumber:D4} '{group.Name}'");

        SendGroupNotification(targetCard, "nano-chat-group-invite-title", ("group", group.Name), ("from", senderName));
        UpdateUIForCard(targetCard);
    }

    /// <summary>
    ///     Handles accepting a pending group invitation.
    /// </summary>
    private void HandleAcceptInvite(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || card.Comp.Number == null)
            return;

        var invite = _nanoChat.GetInvite((card, card.Comp), groupNumber);
        if (invite == null)
            return;

        _nanoChat.RemoveInvite((card, card.Comp), groupNumber);

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) != null)
            return;

        var group = FindGroupDefinition(groupNumber);
        if (group == null)
        {
            UpdateUIForCard(card);
            return;
        }

        if (group.Value.Members.Count >= MaxGroupMembers)
        {
            SendGroupNotification(card, "nano-chat-group-full", ("group", group.Value.Name));
            UpdateUIForCard(card);
            return;
        }

        var memberInfo = GetMemberInfo(card.Comp.Number.Value) ?? new NanoChatMember(card.Comp.Number.Value, "Unknown");
        var members = new List<NanoChatMember>(group.Value.Members) { memberInfo };
        var updatedGroup = group.Value with { Members = members };

        WriteGroupToAllMembers(updatedGroup);

        // Copy recent group history to the new member.
        var sourceCard = GetGroupMemberCards(groupNumber).FirstOrDefault(c => c.Owner != card.Owner);
        if (sourceCard.Owner != EntityUid.Invalid && _nanoChat.GetGroupMessages((sourceCard, sourceCard.Comp), groupNumber) is { } history)
        {
            var recent = history.Count <= MaxGroupHistoryCopied
                ? history
                : history.GetRange(history.Count - MaxGroupHistoryCopied, MaxGroupHistoryCopied);
            _nanoChat.SetGroup((card, card.Comp), updatedGroup);
            foreach (var historicalMessage in recent)
            {
                _nanoChat.AddGroupMessage((card, card.Comp), groupNumber, historicalMessage);
            }
        }
        else
        {
            _nanoChat.SetGroup((card, card.Comp), updatedGroup);
        }

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} joined NanoChat group #{groupNumber:D4} '{group.Value.Name}' (accepted invite from #{invite.Value.FromNumber:D4})");

        SendGroupNotification(card, "nano-chat-group-joined-title", ("group", group.Value.Name));
        UpdateUIForCard(card);
        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles declining a pending group invitation.
    /// </summary>
    private void HandleDeclineInvite(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber)
            return;

        if (_nanoChat.GetInvite((card, card.Comp), groupNumber) == null)
            return;

        _nanoChat.RemoveInvite((card, card.Comp), groupNumber);

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} declined invite to NanoChat group #{groupNumber:D4}");

        UpdateUIForCard(card);
    }

    /// <summary>
    ///     Handles joining a public group without an invite.
    /// </summary>
    private void HandleJoinPublicGroup(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || card.Comp.Number == null)
            return;

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) != null)
            return;

        var group = FindGroupDefinition(groupNumber);
        if (group == null || !group.Value.IsPublic)
        {
            UpdateUIForCard(card);
            return;
        }

        if (group.Value.Members.Count >= MaxGroupMembers)
        {
            SendGroupNotification(card, "nano-chat-group-full", ("group", group.Value.Name));
            UpdateUIForCard(card);
            return;
        }

        var memberInfo = GetMemberInfo(card.Comp.Number.Value) ?? new NanoChatMember(card.Comp.Number.Value, "Unknown");
        var members = new List<NanoChatMember>(group.Value.Members) { memberInfo };
        var updatedGroup = group.Value with { Members = members };

        WriteGroupToAllMembers(updatedGroup);

        var sourceCard = GetGroupMemberCards(groupNumber).FirstOrDefault(c => c.Owner != card.Owner);
        if (sourceCard.Owner != EntityUid.Invalid && _nanoChat.GetGroupMessages((sourceCard, sourceCard.Comp), groupNumber) is { } history)
        {
            var recent = history.Count <= MaxGroupHistoryCopied
                ? history
                : history.GetRange(history.Count - MaxGroupHistoryCopied, MaxGroupHistoryCopied);
            _nanoChat.SetGroup((card, card.Comp), updatedGroup);
            foreach (var historicalMessage in recent)
            {
                _nanoChat.AddGroupMessage((card, card.Comp), groupNumber, historicalMessage);
            }
        }
        else
        {
            _nanoChat.SetGroup((card, card.Comp), updatedGroup);
        }

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} joined public NanoChat group #{groupNumber:D4} '{group.Value.Name}'");

        SendGroupNotification(card, "nano-chat-group-joined-title", ("group", group.Value.Name));
        UpdateUIForCard(card);
        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles leaving a group. If the owner leaves, ownership is transferred
    ///     to the first remaining member; if nobody remains the group is deleted.
    /// </summary>
    private void HandleLeaveGroup(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || card.Comp.Number == null)
            return;

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) is not { } group)
            return;

        var isOwner = group.Owner == card.Comp.Number;

        _nanoChat.RemoveGroup((card, card.Comp), groupNumber);
        UpdateUIForCard(card);

        if (isOwner)
        {
            var remainingMembers = group.Members.Where(m => m.Number != card.Comp.Number).ToList();
            if (remainingMembers.Count == 0)
            {
                // No members left, delete the group everywhere.
                DeleteGroupEverywhere(group);
                _adminLogger.Add(LogType.Action,
                    LogImpact.Medium,
                    $"{ToPrettyString(msg.Actor):user} left NanoChat group #{groupNumber:D4} '{group.Name}' (no members left, group deleted)");
                return;
            }

            var newOwner = remainingMembers[0].Number;
            var updatedGroup = group with { Owner = newOwner, Members = remainingMembers };
            WriteGroupToAllMembers(updatedGroup);

            if (GetCardByNumber(newOwner) is { } newOwnerCard)
            {
                SendGroupNotification(newOwnerCard, "nano-chat-group-owner-title", ("group", group.Name));
                UpdateUIForCard(newOwnerCard);
            }

            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(msg.Actor):user} left NanoChat group #{groupNumber:D4} '{group.Name}', ownership transferred to #{newOwner:D4}");
        }
        else
        {
            var updatedGroup = group with { Members = group.Members.Where(m => m.Number != card.Comp.Number).ToList() };
            WriteGroupToAllMembers(updatedGroup);

            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(msg.Actor):user} left NanoChat group #{groupNumber:D4} '{group.Name}'");
        }

        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles kicking a member from a group (owner only).
    /// </summary>
    private void HandleKickMember(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || msg.TargetNumber is not { } targetNumber || card.Comp.Number == null)
            return;

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) is not { } group)
            return;

        if (group.Owner != card.Comp.Number)
            return;

        if (targetNumber == group.Owner || !group.Members.Any(m => m.Number == targetNumber))
            return;

        var updatedGroup = group with { Members = group.Members.Where(m => m.Number != targetNumber).ToList() };
        WriteGroupToAllMembers(updatedGroup);

        if (GetCardByNumber(targetNumber) is { } targetCard)
        {
            _nanoChat.RemoveGroup((targetCard, targetCard.Comp), groupNumber);
            _nanoChat.RemoveInvite((targetCard, targetCard.Comp), groupNumber);
            SendGroupNotification(targetCard, "nano-chat-group-kicked-title", ("group", group.Name));
            UpdateUIForCard(targetCard);
        }

        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(msg.Actor):user} kicked #{targetNumber:D4} from NanoChat group #{groupNumber:D4} '{group.Name}'");

        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles deleting a group entirely (owner only).
    /// </summary>
    private void HandleDeleteGroup(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber is not { } groupNumber || card.Comp.Number == null)
            return;

        if (_nanoChat.GetGroup((card, card.Comp), groupNumber) is not { } group)
            return;

        if (group.Owner != card.Comp.Number)
            return;

        DeleteGroupEverywhere(group);

        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(msg.Actor):user} deleted NanoChat group #{groupNumber:D4} '{group.Name}'");

        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Removes the group and its messages from every card that has it, notifying members.
    ///     Also removes any stale invitations to the group.
    /// </summary>
    private void DeleteGroupEverywhere(NanoChatGroup group)
    {
        foreach (var memberCard in GetGroupMemberCards(group.Number))
        {
            _nanoChat.RemoveGroup((memberCard, memberCard.Comp), group.Number);
            SendGroupNotification(memberCard, "nano-chat-group-deleted-title", ("group", group.Name));
            UpdateUIForCard(memberCard);
        }

        // Remove stale invitations from all cards, not just members.
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (_nanoChat.RemoveInvite((uid, card), group.Number))
                UpdateUIForCard(uid);
        }
    }

    /// <summary>
    ///     Sends a PDA notification to the card's owner.
    /// </summary>
    private void SendGroupNotification(Entity<NanoChatCardComponent> card, string titleKey, params (string, object)[] args)
    {
        if (card.Comp.PdaUid is not {} pdaUid ||
            !TryComp<CartridgeLoaderComponent>(pdaUid, out var loader))
            return;

        var title = Loc.GetString(titleKey, args);
        _cartridge.SendNotification(pdaUid, title, "", loader);
    }

    /// <summary>
    ///     Generates a unique group number.
    /// </summary>
    private uint GenerateGroupNumber()
    {
        while (true)
        {
            var candidate = (uint)_random.Next(1000, 10000);
            if (FindGroupDefinition(candidate) == null)
                return candidate;
        }
    }

    /// <summary>
    ///     Gets a <see cref="NanoChatMember" /> for the given NanoChat number.
    /// </summary>
    private NanoChatMember? GetMemberInfo(uint number)
    {
        var info = GetCardInfo(number);
        if (info == null)
            return null;

        return new NanoChatMember(info.Value.Number, info.Value.Name, info.Value.JobTitle);
    }

    /// <summary>
    ///     Ensures a recipient exists in the sender's contacts.
    /// </summary>
    /// <param name="card">The card to check contacts for</param>
    /// <param name="recipientNumber">The recipient's number to check</param>
    /// <returns>True if the recipient exists or was created successfully</returns>
    private bool EnsureRecipientExists(Entity<NanoChatCardComponent> card, uint recipientNumber)
    {
        return _nanoChat.EnsureRecipientExists((card, card.Comp), recipientNumber, GetCardInfo(recipientNumber));
    }

    /// <summary>
    ///     Attempts to deliver a message to recipients.
    /// </summary>
    /// <param name="sender">The sending cartridge entity</param>
    /// <param name="recipientNumber">The recipient's number</param>
    /// <returns>Tuple containing delivery status and recipients if found.</returns>
    private (bool failed, List<Entity<NanoChatCardComponent>> recipient) AttemptMessageDelivery(
        Entity<NanoChatCartridgeComponent> sender,
        uint recipientNumber)
    {
        // First verify we can send from this device
        var channel = _prototype.Index(sender.Comp.RadioChannel);
        var sendAttemptEvent = new RadioSendAttemptEvent(channel, sender);
        RaiseLocalEvent(ref sendAttemptEvent);
        if (sendAttemptEvent.Cancelled)
            return (true, new List<Entity<NanoChatCardComponent>>());

        var foundRecipients = new List<Entity<NanoChatCardComponent>>();

        // Find all cards with matching number
        var cardQuery = EntityQueryEnumerator<NanoChatCardComponent>();
        while (cardQuery.MoveNext(out var cardUid, out var card))
        {
            if (card.Number != recipientNumber)
                continue;

            foundRecipients.Add((cardUid, card));
        }

        if (foundRecipients.Count == 0)
            return (true, foundRecipients);

        // Now check if any of these cards can receive
        var deliverableRecipients = new List<Entity<NanoChatCardComponent>>();
        foreach (var recipient in foundRecipients)
        {
            // Find any cartridges that have this card
            var cartridgeQuery = EntityQueryEnumerator<NanoChatCartridgeComponent, ActiveRadioComponent>();
            while (cartridgeQuery.MoveNext(out var receiverUid, out var receiverCart, out _))
            {
                if (receiverCart.Card != recipient.Owner)
                    continue;

                // Check if devices are on same station/map
                var recipientStation = _station.GetOwningStation(receiverUid);
                var senderStation = _station.GetOwningStation(sender);

                // Both entities must be on a station
                if (recipientStation == null || senderStation == null)
                    continue;

                // Must be on same map/station unless long range allowed
                if (!channel.LongRange && recipientStation != senderStation)
                    continue;

                // Needs telecomms
                if (!HasActiveServer(senderStation.Value) || !HasActiveServer(recipientStation.Value))
                    continue;

                // Check if recipient can receive
                var receiveAttemptEv = new RadioReceiveAttemptEvent(channel, sender, receiverUid);
                RaiseLocalEvent(ref receiveAttemptEv);
                if (receiveAttemptEv.Cancelled)
                    continue;

                // Found valid cartridge that can receive
                deliverableRecipients.Add(recipient);
                break; // Only need one valid cartridge per card
            }
        }

        return (deliverableRecipients.Count == 0, deliverableRecipients);
    }

    /// <summary>
    ///     Checks if there are any active telecomms servers on the given station
    /// </summary>
    private bool HasActiveServer(EntityUid station)
    {
        // I have no idea why this isn't public in the RadioSystem
        var query =
            EntityQueryEnumerator<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent>();

        while (query.MoveNext(out var uid, out _, out _, out var power))
        {
            if (_station.GetOwningStation(uid) == station && power.Powered)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Delivers a message to the recipient and handles associated notifications.
    /// </summary>
    /// <param name="sender">The sender's card entity</param>
    /// <param name="recipient">The recipient's card entity</param>
    /// <param name="message">The <see cref="NanoChatMessage" /> to deliver</param>
    private void DeliverMessageToRecipient(Entity<NanoChatCardComponent> sender,
        Entity<NanoChatCardComponent> recipient,
        NanoChatMessage message)
    {
        var senderNumber = sender.Comp.Number;
        if (senderNumber == null)
            return;

        // Always try to get and add sender info to recipient's contacts
        if (!EnsureRecipientExists(recipient, senderNumber.Value))
            return;

        _nanoChat.AddMessage((recipient, recipient.Comp), senderNumber.Value, message with { DeliveryFailed = false });


        HandleUnreadNotification(recipient, message, (uint) senderNumber);

        var msgEv = new NanoChatMessageReceivedEvent(recipient);
        RaiseLocalEvent(ref msgEv);
        UpdateUIForCard(recipient);
    }

    /// <summary>
    ///     Handles unread message notifications and updates unread status.
    /// </summary>
    private void HandleUnreadNotification(Entity<NanoChatCardComponent> recipient,
        NanoChatMessage message,
        uint senderNumber)
    {
        // Get sender name from contacts or fall back to number
        var recipients = _nanoChat.GetRecipients((recipient, recipient.Comp));
        var senderName = recipients.TryGetValue(message.SenderId, out var senderRecipient)
            ? senderRecipient.Name
            : $"#{message.SenderId:D4}";
        var hasSelectedCurrentChat = _nanoChat.GetCurrentChat((recipient, recipient.Comp)) == senderNumber;

        // Update unread status
        if (!hasSelectedCurrentChat)
            _nanoChat.SetRecipient((recipient, recipient.Comp),
                message.SenderId,
                senderRecipient with { HasUnread = true });

        if (recipient.Comp.NotificationsMuted ||
            recipient.Comp.PdaUid is not {} pdaUid ||
            !TryComp<CartridgeLoaderComponent>(pdaUid, out var loader) ||
            // Don't notify if the recipient has the NanoChat program open with this chat selected.
            (hasSelectedCurrentChat &&
                _ui.IsUiOpen(pdaUid, PdaUiKey.Key) &&
                HasComp<NanoChatCartridgeComponent>(loader.ActiveProgram)))
            return;

        _cartridge.SendNotification(pdaUid,
            Loc.GetString("nano-chat-new-message-title", ("sender", senderName)),
            Loc.GetString("nano-chat-new-message-body", ("message", TruncateMessage(message.Content))),
            loader);
    }

    /// <summary>
    ///     Updates the UI for any PDAs containing the specified card.
    /// </summary>
    private void UpdateUIForCard(EntityUid cardUid)
    {
        // Find any PDA containing this card and update its UI
        var query = EntityQueryEnumerator<NanoChatCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (comp.Card != cardUid || cartridge.LoaderUid == null)
                continue;

            UpdateUI((uid, comp), cartridge.LoaderUid.Value);
        }
    }

    /// <summary>
    ///     Updates the UI for all PDAs containing a NanoChat cartridge.
    /// </summary>
    private void UpdateUIForAllCards()
    {
        // Find any PDA containing this card and update its UI
        var query = EntityQueryEnumerator<NanoChatCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (cartridge.LoaderUid is { } loader)
                UpdateUI((uid, comp), loader);
        }
    }

    /// <summary>
    ///     Gets the <see cref="NanoChatRecipient" /> for a given NanoChat number.
    /// </summary>
    private NanoChatRecipient? GetCardInfo(uint number)
    {
        // Find card with this number to get its info
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Number != number)
                continue;

            // Try to get job title from ID card if possible
            string? jobTitle = null;
            var name = "Unknown";
            if (TryComp<IdCardComponent>(uid, out var idCard))
            {
                jobTitle = idCard.LocalizedJobTitle;
                name = idCard.FullName ?? name;
            }

            return new NanoChatRecipient(number, name, jobTitle);
        }

        return null;
    }

    /// <summary>
    ///     Truncates a message to the notification maximum length.
    /// </summary>
    private static string TruncateMessage(string message)
    {
        return message.Length <= NotificationMaxLength
            ? message
            : message[..(NotificationMaxLength - 4)] + " [...]";
    }

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        _cartridge.RegisterBackgroundProgram(args.Loader, ent);
        UpdateUI(ent, args.Loader);
    }

    private void UpdateUI(Entity<NanoChatCartridgeComponent> ent, EntityUid loader)
    {
        List<NanoChatRecipient>? contacts;
        EntityUid? station = null;
        if (_station.GetOwningStation(loader) is { } owningStation)
        {
            station = owningStation;
            ent.Comp.Station = owningStation;

            contacts = [];

            var query = AllEntityQuery<NanoChatCardComponent, IdCardComponent>();
            while (query.MoveNext(out var entityId, out var nanoChatCard, out var idCardComponent))
            {
                if (nanoChatCard.ListNumber && nanoChatCard.Number is uint nanoChatNumber && idCardComponent.FullName is string fullName && _station.GetOwningStation(entityId) == station)
                {
                    contacts.Add(new NanoChatRecipient(nanoChatNumber, fullName));
                }
            }
            contacts.Sort((contactA, contactB) => string.CompareOrdinal(contactA.Name, contactB.Name));
        }
        else
        {
            contacts = null;
        }

        var recipients = new Dictionary<uint, NanoChatRecipient>();
        var messages = new Dictionary<uint, List<NanoChatMessage>>();
        var groups = new Dictionary<uint, NanoChatGroup>();
        var groupMessages = new Dictionary<uint, List<NanoChatMessage>>();
        var invites = new List<NanoChatGroupInvite>();
        var publicGroups = new List<NanoChatGroupInfo>();
        uint? currentChat = null;
        uint ownNumber = 0;
        var maxRecipients = 50;
        var notificationsMuted = false;
        var listNumber = false;

        if (ent.Comp.Card != null && TryComp<NanoChatCardComponent>(ent.Comp.Card, out var card))
        {
            recipients = card.Recipients;
            messages = card.Messages;
            groups = card.Groups;
            groupMessages = card.GroupMessages;
            invites = card.Invites.Values.ToList();
            currentChat = card.CurrentChat;
            ownNumber = card.Number ?? 0;
            maxRecipients = card.MaxRecipients;
            notificationsMuted = card.NotificationsMuted;
            listNumber = card.ListNumber;

            publicGroups = GetPublicGroups((ent.Comp.Card.Value, card), station);
        }

        var state = new NanoChatUiState(recipients,
            messages,
            groups,
            groupMessages,
            invites,
            publicGroups,
            contacts,
            currentChat,
            ownNumber,
            maxRecipients,
            notificationsMuted,
            listNumber);
        _cartridge.UpdateCartridgeUiState(loader, state);
    }

    /// <summary>
    ///     Compiles the list of public groups visible to the given card:
    ///     same station only, excluding groups the viewer is already in.
    /// </summary>
    private List<NanoChatGroupInfo> GetPublicGroups(Entity<NanoChatCardComponent> viewerCard, EntityUid? viewerStation)
    {
        var result = new List<NanoChatGroupInfo>();
        var seen = new HashSet<uint>();

        if (viewerStation == null)
            return result;

        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var cardUid, out var card))
        {
            if (_station.GetOwningStation(cardUid) != viewerStation)
                continue;

            foreach (var group in card.Groups.Values)
            {
                if (!group.IsPublic || seen.Contains(group.Number))
                    continue;

                if (_nanoChat.GetGroup((viewerCard, viewerCard.Comp), group.Number) != null)
                    continue;

                var groupNumber = group.Number;

                seen.Add(groupNumber);

                var ownerName = GetCardInfo(group.Owner)?.Name;
                result.Add(new NanoChatGroupInfo(groupNumber, group.Name, ownerName, group.Members.Count));
            }
        }

        result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return result;
    }
}
