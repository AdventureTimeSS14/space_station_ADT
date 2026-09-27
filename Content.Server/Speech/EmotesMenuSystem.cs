using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.ADT.Actions.Events;
using Content.Shared.ADT.Chat;

namespace Content.Server.Speech;

public sealed partial class EmotesMenuSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    // ADT-Tweak-Start
    public static readonly EntProtoId EmoteBindActionProtoId = "ADTActionEmoteBindBase";
    // ADT-Tweak-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayEmoteMessage>(OnPlayEmote);
        // ADT-Tweak-Start
        SubscribeAllEvent<RequestBindEmoteMessage>(OnRequestBindEmote);
        SubscribeLocalEvent<PlayEmoteActionEvent>(OnPlayEmoteAction);
        // ADT-Tweak-End
    }

    private void OnPlayEmote(PlayEmoteMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!player.HasValue)
            return;

        if (!_prototypeManager.Resolve(msg.ProtoId, out var proto) || proto.ChatTriggers.Count == 0)
            return;

        _chat.TryEmoteWithChat(player.Value, msg.ProtoId);
    }

    // ADT-Tweak-Start
    private void OnRequestBindEmote(RequestBindEmoteMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!player.HasValue)
            return;

        if (!_prototypeManager.Resolve(msg.ProtoId, out var proto) || proto.ChatTriggers.Count == 0)
            return;

        var name = Loc.GetString(proto.Name);

        foreach (var existing in _actions.GetActions(player.Value))
        {
            if (_actions.GetEvent(existing) is PlayEmoteActionEvent ev && ev.ProtoId.Id == proto.ID)
            {
                _popup.PopupEntity(Loc.GetString("emote-menu-already-bound", ("emote", name)), player.Value, player.Value);
                _actions.RemoveAction(player.Value, existing!);
                QueueDel(existing.Owner);
                return;
            }
        }

        EntityUid? actionId = default!;
        if (!_actions.AddAction(player.Value, ref actionId, EmoteBindActionProtoId, player.Value))
            return;

        if (_actions.GetAction(actionId) is not { } action)
            return;

        var metaDataCache = MetaData(action);
        _metaData.SetEntityName(action, name, metaDataCache);
        _actions.SetIcon((action, action.Comp), proto.Icon);
        _actions.SetEvent(action, new PlayEmoteActionEvent { ProtoId = proto.ID });

        _popup.PopupEntity(Loc.GetString("emote-menu-bound", ("emote", name)), player.Value, player.Value);
    }

    private void OnPlayEmoteAction(PlayEmoteActionEvent ev)
    {
        if (ev.ProtoId.Id.Length == 0)
            return;

        ev.Handled = _chat.TryEmoteWithChat(ev.Performer, ev.ProtoId);
    }
    // ADT-Tweak-End
}
