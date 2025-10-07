using Content.Server.Abilities.Mime;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.ADT.Mime;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Speech.Muting;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mime;

public sealed class MimeThroatPunchSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimeThroatPunchComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MimeThroatPunchComponent, MimeThroatPunchActionEvent>(OnThroatPunch);
    }

    private void OnComponentInit(EntityUid uid, MimeThroatPunchComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ThroatPunchActionEntity, component.ThroatPunchAction, uid);
    }

    private void OnThroatPunch(EntityUid uid, MimeThroatPunchComponent component, MimeThroatPunchActionEvent args)
    {
        if (!TryComp<MimePowersComponent>(uid, out var mimePowers))
            return;

        if (!mimePowers.Enabled)
            return;

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        var target = args.Target;

        var mutedComp = EnsureComp<MutedComponent>(target);

        _alertsSystem.ShowAlert(target, "Muted");

        var ev = new RemoveMutedEvent(target);
        AddComp<RemoveMutedOnDelayComponent>(target).RemoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.MuteDuration);

        var message = Loc.GetString("mime-throat-punch-emote", ("entity", uid));
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", Name(uid)),
            ("message", message));

        if (_playerManager.TryGetSessionByEntity(uid, out var session))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, uid, false, session.Channel);
        }

        _popupSystem.PopupEntity(Loc.GetString("mime-throat-punch-target", ("duration", component.MuteDuration)), target, target);

        _popupSystem.PopupEntity(Loc.GetString("mime-throat-punch-user", ("target", target)), uid, uid);

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new List<EntityUid>();

        var query = EntityQueryEnumerator<RemoveMutedOnDelayComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.RemoveTime)
                continue;

            toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            RemComp<MutedComponent>(uid);
            RemComp<RemoveMutedOnDelayComponent>(uid);
            _alertsSystem.ClearAlert(uid, "Muted");
        }
    }
}

[RegisterComponent]
public sealed partial class RemoveMutedOnDelayComponent : Component
{
    public TimeSpan RemoveTime;
}

public sealed class RemoveMutedEvent : EntityEventArgs
{
    public EntityUid Target;

    public RemoveMutedEvent(EntityUid target)
    {
        Target = target;
    }
}
