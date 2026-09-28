using Content.Shared.ADT.Mime;
using Content.Shared.Abilities.Mime;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Mime;

public sealed class MimeSilenceSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private Content.Shared.StatusEffectNew.StatusEffectsSystem _statusEffects = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private static readonly EntProtoId MuteEffect = "StatusEffectMuted";

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
            if (entity == uid || !HasComp<HumanoidProfileComponent>(entity))
                continue;

            _statusEffects.TryUpdateStatusEffectDuration(entity, MuteEffect, TimeSpan.FromSeconds(args.MuteDuration));
            _popupSystem.PopupEntity(Loc.GetString("mime-silence-target", ("duration", args.MuteDuration)), entity, entity);
        }

        args.Handled = true;
    }
}
