using Content.Shared.Abilities.Mime;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.ADT.Mime;
using Content.Shared.Chat;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mime;

public sealed class MimeFingerGunSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

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

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        var xform = Transform(uid);
        var fromCoords = xform.Coordinates;
        var toCoords = args.Target;
        var userVelocity = _physics.GetMapLinearVelocity(uid);

        var fromMap = _transform.ToMapCoordinates(fromCoords);
        var spawnCoords = _mapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? _transform.WithEntityId(fromCoords, gridUid)
            : new(_mapSystem.GetMap(fromMap.MapId), fromMap.Position);

        var projectile = Spawn("ADTMimeInvisibleBullet", spawnCoords);
        var direction = _transform.ToMapCoordinates(toCoords).Position -
                         _transform.ToMapCoordinates(spawnCoords).Position;
        _gunSystem.ShootProjectile(projectile, direction, userVelocity, uid, uid);

        var message = Loc.GetString("mime-finger-gun-emote", ("entity", uid));
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", Name(uid)),
            ("message", message));

        if (_playerManager.TryGetSessionByEntity(uid, out var session))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, uid, false, session.Channel);
        }

        var soundVariants = new[]
        {
            Loc.GetString("mime-finger-gun-popup-1"),
            Loc.GetString("mime-finger-gun-popup-2"),
            Loc.GetString("mime-finger-gun-popup-3"),
            Loc.GetString("mime-finger-gun-popup-4")
        };
        var randomSound = _random.Pick(soundVariants);
        _popupSystem.PopupEntity(randomSound, uid);

        args.Handled = true;
    }
}
