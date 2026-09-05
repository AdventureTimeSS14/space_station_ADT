using Content.Server.ADT.CartridgeLoader.Cartridges;
using Content.Server.Actions;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.NanoChat;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.ADT.NanoChat;

public sealed class StationAiNanoChatSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly NanoChatCartridgeSystem _nanoChatCartridge = default!;

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
                _nanoChatCartridge.NewChat(card, msg.RecipientNumber, msg.Content, msg.RecipientJob, msg.Actor);
                break;
            case NanoChatUiMessageType.SelectChat:
                _nanoChatCartridge.SelectChat(card, msg.RecipientNumber);
                break;
            case NanoChatUiMessageType.CloseChat:
                _nanoChatCartridge.CloseChat(card);
                break;
            case NanoChatUiMessageType.ToggleMute:
                _nanoChatCartridge.ToggleMute(card);
                break;
            case NanoChatUiMessageType.DeleteChat:
                _nanoChatCartridge.DeleteChat(card, msg.RecipientNumber, msg.Actor);
                break;
            case NanoChatUiMessageType.SendMessage:
                _nanoChatCartridge.SendMessage(ent.Owner, card, msg.RecipientNumber, msg.Content, ent.Comp.RadioChannel);
                break;
            case NanoChatUiMessageType.ToggleListNumber:
                _nanoChatCartridge.ToggleListNumber(card);
                break;
        }

        UpdateUi(ent);
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
