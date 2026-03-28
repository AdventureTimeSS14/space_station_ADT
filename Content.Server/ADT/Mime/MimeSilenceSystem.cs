using Content.Shared.ADT.Mime;
using Content.Shared.Abilities.Mime;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.Speech.Muting;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mime;

public sealed class MimeSilenceSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimeSilenceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MimeSilenceComponent, MimeSilenceActionEvent>(OnSilence);
    }

    private void OnComponentInit(EntityUid uid, MimeSilenceComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SilenceActionEntity, component.SilenceAction, uid);
    }

    private void OnSilence(EntityUid uid, MimeSilenceComponent component, MimeSilenceActionEvent args)
    {
        if (!TryComp<MimePowersComponent>(uid, out var mimePowers) || !mimePowers.Enabled)
            return;

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        var message = Loc.GetString("mime-silence-emote", ("entity", uid));
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", Name(uid)), ("message", message));

        if (_playerManager.TryGetSessionByEntity(uid, out var session))
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, uid, false, session.Channel);

        foreach (var entity in _lookup.GetEntitiesInRange(uid, args.Range, LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (entity == uid || !HasComp<HumanoidAppearanceComponent>(entity))
                continue;

            EnsureComp<MutedComponent>(entity);
            _alertsSystem.ShowAlert(entity, "Muted");
            _popupSystem.PopupEntity(Loc.GetString("mime-silence-target", ("duration", args.MuteDuration)), entity, entity);
            AddComp<MutedTimerComponent>(entity).ExpiryTime = _timing.CurTime + TimeSpan.FromSeconds(args.MuteDuration);
        }

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MutedTimerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ExpiryTime)
            {
                RemComp<MutedComponent>(uid);
                RemComp<MutedTimerComponent>(uid);
                _alertsSystem.ClearAlert(uid, "Muted");
            }
        }
    }
}

[RegisterComponent]
public sealed partial class MutedTimerComponent : Component
{
    public TimeSpan ExpiryTime;
}
