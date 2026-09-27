using Content.Server.ADT.CartridgeLoader.Cartridges;
using Content.Server.Actions;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.NanoChat;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.NanoChat;

public sealed class StationAiNanoChatSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly NanoChatCartridgeSystem _nanoChatCartridge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiNanoChatComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAiNanoChatComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StationAiNanoChatComponent, StationAiNanoChatActionEvent>(OnAction);
        SubscribeLocalEvent<StationAiNanoChatComponent, BoundUIOpenedEvent>(OnUiOpened);

        Subs.BuiEvents<StationAiNanoChatComponent>(StationAiNanoChatUiKey.Key, subs =>
        {
            subs.Event<StationAiNanoChatUiMessage>(OnMessage);
        });
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

        _uiSystem.TryToggleUi(uid, StationAiNanoChatUiKey.Key, args.Performer);
        args.Handled = true;
    }

    private void OnUiOpened(Entity<StationAiNanoChatComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!StationAiNanoChatUiKey.Key.Equals(args.UiKey))
            return;

        _nanoChatCartridge.UpdateStationAiUi(ent);
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

        _nanoChatCartridge.UpdateStationAiUi(ent);
    }
}
