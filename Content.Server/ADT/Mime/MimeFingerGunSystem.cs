using Content.Server.Chat.Managers;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Abilities.Mime;
using Content.Shared.Actions;
using Content.Shared.ADT.Mime;
using Content.Shared.Chat;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Mime;

public sealed class MimeFingerGunSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimeFingerGunComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MimeFingerGunComponent, MimeFingerGunActionEvent>(OnFingerGun);
    }

    private void OnComponentInit(EntityUid uid, MimeFingerGunComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.FingerGunActionEntity, component.FingerGunAction, uid);
    }

    private void OnFingerGun(EntityUid uid, MimeFingerGunComponent component, MimeFingerGunActionEvent args)
    {
        if (!TryComp<MimePowersComponent>(uid, out var mimePowers))
            return;

        if (!mimePowers.Enabled)
            return;

        if (component.FingerGunEntity.HasValue && Exists(component.FingerGunEntity.Value))
        {
            args.Handled = true;
            return;
        }

        var gunPrototype = component.FingerGunPrototype;
        if (!_prototypeManager.TryIndex<EntityPrototype>(gunPrototype, out var prototype))
        {
            Log.Error($"[MimeFingerGun] Не найден прототип {gunPrototype}");
            args.Handled = true;
            return;
        }

        var gunEntity = Spawn(gunPrototype, Transform(uid).Coordinates);
        component.FingerGunEntity = gunEntity;
        Dirty(uid, component);

        if (TryComp<MimeFingerGunItemComponent>(gunEntity, out var gunItem))
        {
            gunItem.MimeUid = uid;
            Dirty(gunEntity, gunItem);

            ResetRevolver(gunEntity);
        }

        if (!_handsSystem.TryPickupAnyHand(uid, gunEntity))
        {
            _popupSystem.PopupEntity(Loc.GetString("mime-finger-gun-no-hands"), uid, uid);
            Del(gunEntity);
            component.FingerGunEntity = null;
            Dirty(uid, component);
            args.Handled = true;
            return;
        }

        ShowFingerGunEmote(uid);

        args.Handled = true;
    }

    private void ResetRevolver(EntityUid uid)
    {
        if (!TryComp<RevolverAmmoProviderComponent>(uid, out var revolver))
            return;

        for (var i = 0; i < revolver.Chambers.Length; i++)
        {
            revolver.Chambers[i] = true;
        }

        Dirty(uid, revolver);
    }

    private void ShowFingerGunEmote(EntityUid uid)
    {
        var message = Loc.GetString("mime-finger-gun-emote", ("entity", uid));
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", Name(uid)),
            ("message", message));

        if (_playerManager.TryGetSessionByEntity(uid, out var session))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, uid, false, session.Channel);
        }
    }
}
