using System.Linq;
using Content.Server.ADT.CartridgeLoader.Cartridges;
using Content.Server.Administration.Logs;
using Content.Server.Actions;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.NanoChat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.NanoChat;

public sealed class StationAiNanoChatSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedNanoChatSystem _nanoChat = default!;
    [Dependency] private readonly NanoChatCartridgeSystem _nanoChatCartridge = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiNanoChatComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAiNanoChatComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StationAiNanoChatComponent, StationAiNanoChatActionEvent>(OnAction);
        SubscribeLocalEvent<StationAiNanoChatComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NanoChatCardComponent, NanoChatMessageReceivedEvent>(OnCardMessageReceived);

        Subs.BuiEvents<StationAiNanoChatComponent>(StationAiNanoChatUiKey.Key, subs =>
        {
            subs.Event<StationAiNanoChatUiMessage>(OnMessage);
        });
    }

    private void OnUiOpened(Entity<StationAiNanoChatComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!StationAiNanoChatUiKey.Key.Equals(args.UiKey))
            return;

        UpdateUi(ent);
    }

    private void OnCardMessageReceived(Entity<NanoChatCardComponent> ent, ref NanoChatMessageReceivedEvent args)
    {
        if (!HasComp<StationAiNanoChatComponent>(ent.Owner))
            return;

        UpdateUi((ent.Owner, Comp<StationAiNanoChatComponent>(ent.Owner)));
    }

    private void OnMapInit(EntityUid uid, StationAiNanoChatComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnShutdown(EntityUid uid, StationAiNanoChatComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

    private void OnAction(EntityUid uid, StationAiNanoChatComponent comp, StationAiNanoChatActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, StationAiNanoChatUiKey.Key, actor.Owner);
        args.Handled = true;
    }

    private void OnMessage(Entity<StationAiNanoChatComponent> ent, ref StationAiNanoChatUiMessage msg)
    {
        if (!TryComp<NanoChatCardComponent>(ent.Owner, out var cardComp))
            return;

        var card = new Entity<NanoChatCardComponent>(ent.Owner, cardComp);

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
        }

        UpdateUi(ent);
    }

    private void HandleNewChat(Entity<NanoChatCardComponent> card, StationAiNanoChatUiMessage msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || msg.RecipientNumber == card.Comp.Number)
            return;

        var name = msg.Content.Trim();
        var jobTitle = string.IsNullOrWhiteSpace(msg.RecipientJob) ? null : msg.RecipientJob.Trim();

        _nanoChat.SetRecipient((card, card.Comp), msg.RecipientNumber.Value, new NanoChatRecipient(msg.RecipientNumber.Value, name, jobTitle));

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} created new NanoChat conversation with #{msg.RecipientNumber:D4} ({name})");

        var recipientEv = new NanoChatRecipientUpdatedEvent(card);
        RaiseLocalEvent(ref recipientEv);
    }

    private void HandleSelectChat(Entity<NanoChatCardComponent> card, StationAiNanoChatUiMessage msg)
    {
        if (msg.RecipientNumber == null)
            return;

        _nanoChat.SetCurrentChat((card, card.Comp), msg.RecipientNumber);

        if (_nanoChat.GetRecipient((card, card.Comp), msg.RecipientNumber.Value) is { } recipient)
        {
            _nanoChat.SetRecipient((card, card.Comp),
                msg.RecipientNumber.Value,
                recipient with { HasUnread = false });
        }
    }

    private void HandleCloseChat(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetCurrentChat((card, card.Comp), null);
    }

    private void HandleDeleteChat(Entity<NanoChatCardComponent> card, StationAiNanoChatUiMessage msg)
    {
        if (msg.RecipientNumber == null)
            return;

        var deleted = _nanoChat.TryDeleteChat((card, card.Comp), msg.RecipientNumber.Value, true);

        if (!deleted)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} deleted NanoChat conversation with #{msg.RecipientNumber:D4}");
    }

    private void HandleToggleMute(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetNotificationsMuted((card, card.Comp), !_nanoChat.GetNotificationsMuted((card, card.Comp)));
    }

    private void HandleToggleListNumber(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetListNumber((card, card.Comp), !_nanoChat.GetListNumber((card, card.Comp)));
    }

    private void HandleSendMessage(Entity<StationAiNanoChatComponent> ent,
        Entity<NanoChatCardComponent> card,
        StationAiNanoChatUiMessage msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || card.Comp.Number == null)
            return;

        if (!_nanoChat.EnsureRecipientExists((card, card.Comp), msg.RecipientNumber.Value,
                _nanoChatCartridge.GetCardInfo(msg.RecipientNumber.Value)))
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

        var (deliveryFailed, recipients) = _nanoChatCartridge.AttemptMessageDeliveryInternal(
            ent.Owner, msg.RecipientNumber.Value, ent.Comp.RadioChannel);

        message = message with { DeliveryFailed = deliveryFailed };

        _nanoChat.AddMessage((card, card.Comp), msg.RecipientNumber.Value, message);

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
            _nanoChatCartridge.DeliverMessageToRecipient(card, recipient, message);
        }
    }

    private void UpdateUi(Entity<StationAiNanoChatComponent> ent)
    {
        if (!TryComp<NanoChatCardComponent>(ent.Owner, out var card))
            return;

        List<NanoChatRecipient>? contacts = null;
        if (_station.GetOwningStation(ent.Owner) is { } station)
        {
            contacts = new List<NanoChatRecipient>();

            var query = AllEntityQuery<NanoChatCardComponent, IdCardComponent>();
            while (query.MoveNext(out var entityId, out var nanoChatCard, out var idCardComponent))
            {
                if (nanoChatCard.ListNumber && nanoChatCard.Number is uint nanoChatNumber &&
                    idCardComponent.FullName is string fullName &&
                    _station.GetOwningStation(entityId) == station)
                {
                    contacts.Add(new NanoChatRecipient(nanoChatNumber, fullName));
                }
            }
            contacts.Sort((contactA, contactB) => string.CompareOrdinal(contactA.Name, contactB.Name));
        }

        var state = new NanoChatUiState(card.Recipients,
            card.Messages,
            contacts,
            card.CurrentChat,
            card.Number ?? 0,
            card.MaxRecipients,
            card.NotificationsMuted,
            card.ListNumber);

        _uiSystem.SetUiState(ent.Owner, StationAiNanoChatUiKey.Key, state);
    }
}
