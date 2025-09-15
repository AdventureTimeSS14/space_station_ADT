using Content.Server.ADT.Mime;
using Content.Shared.ADT.Mime;
using Content.Server.Chat.Managers;
using Robust.Shared.Random;
using Content.Server.Actions;
using Robust.Server.Player;
using Content.Shared.Chat;
using Robust.Shared.Utility;
using Content.Shared.Hands.Components;
using Content.Server.Popups;
using Content.Server.Hands.Systems;

public sealed class MimeBaloonSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimeBaloonComponent, SpawnBaloonEvent>(OnSpawnBaloonMime);
        SubscribeLocalEvent<MimeBaloonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MimeBaloonComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(EntityUid uid, MimeBaloonComponent component, ComponentInit args)
    {
        _action.AddAction(uid, ref component.Action, "ADTActionBaloonOfMime");
    }

    private void OnRemove(EntityUid uid, MimeBaloonComponent component, ComponentRemove args)
    {
        _action.RemoveAction(uid, component.Action);
    }

    private void OnSpawnBaloonMime(EntityUid uid, MimeBaloonComponent component, SpawnBaloonEvent args)
    {
        if (!TryComp<HandsComponent>(args.Performer, out var handsComponent))
            return;

        var randomPickPrototype = _random.Pick(component.ListPrototypesBaloon);
        var balloon = Spawn(randomPickPrototype, Transform(args.Performer).Coordinates);

        if (!_handsSystem.TryPickupAnyHand(args.Performer, balloon))
        {
            QueueDel(balloon);
            _popup.PopupEntity(Loc.GetString("mime-baloon-fail"), args.Performer, args.Performer);
            return;
        }

        var message = Loc.GetString("mime-baloon-emote", ("entity", uid));
        var name = Name(args.Performer);
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", args.Performer),
            ("message", FormattedMessage.RemoveMarkupOrThrow(message)));

        if (_playerManager.TryGetSessionByEntity(args.Performer, out var session))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, args.Performer, false, session.Channel);
        }

        _action.StartUseDelay(component.Action);
    }
}
