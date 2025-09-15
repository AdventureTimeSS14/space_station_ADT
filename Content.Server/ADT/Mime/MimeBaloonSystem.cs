using Content.Server.ADT.Mime;
using Content.Shared.ADT.Mime;
using Content.Server.Chat.Managers;
using Robust.Shared.Random;
using Content.Server.Actions;
using Robust.Server.Player;
using Content.Shared.Chat;
using Robust.Shared.Utility;

public sealed class MimeBaloonSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

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
        var xform = Transform(args.Performer);

        var message = Loc.GetString("mime-baloon-popup", ("entity", uid));
        var name = Name(args.Performer);
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", args.Performer),
            ("message", FormattedMessage.RemoveMarkupOrThrow(message)));

        if (_playerManager.TryGetSessionByEntity(args.Performer, out var session))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, args.Performer, false, session.Channel);
        }

        var randomPickPrototype = _random.Pick(component.ListPrototypesBaloon);
        Spawn(randomPickPrototype, xform.Coordinates);
        _action.StartUseDelay(component.Action);
    }
}
