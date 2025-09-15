using Content.Server.Popups;
using Content.Shared.Abilities.Mime;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;
//ADT-Tweak-Start
using Robust.Shared.Utility;
using Content.Server.Chat.Managers;
using Content.Server.Hands.Systems;
using Content.Shared.ADT.Mime;
using Content.Shared.Chat;
using Content.Shared.Hands.Components;
using Robust.Server.Player;
using Robust.Shared.Random;
//ADT-Tweak-End

namespace Content.Server.Abilities.Mime
{
    public sealed class MimePowersSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        //ADT-Tweak-Start
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        //ADT-Tweak-End

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);
            SubscribeLocalEvent<MimePowersComponent, SpawnBaloonEvent>(OnSpawnBalloon); //ADT-Tweak

            SubscribeLocalEvent<MimePowersComponent, BreakVowAlertEvent>(OnBreakVowAlert);
            SubscribeLocalEvent<MimePowersComponent, RetakeVowAlertEvent>(OnRetakeVowAlert);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            // Queue to track whether mimes can retake vows yet

            var query = EntityQueryEnumerator<MimePowersComponent>();
            while (query.MoveNext(out var uid, out var mime))
            {
                if (!mime.VowBroken || mime.ReadyToRepent)
                    continue;

                if (_timing.CurTime < mime.VowRepentTime)
                    continue;

                mime.ReadyToRepent = true;
                _popupSystem.PopupEntity(Loc.GetString("mime-ready-to-repent"), uid, uid);
            }
        }

        private void OnComponentInit(EntityUid uid, MimePowersComponent component, ComponentInit args)
        {
            EnsureComp<MutedComponent>(uid);
            if (component.PreventWriting)
            {
                EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
                illiterateComponent.FailWriteMessage = component.FailWriteMessage;
                Dirty(uid, illiterateComponent);
            }

            _alertsSystem.ShowAlert(uid, component.VowAlert);
            _actionsSystem.AddAction(uid, ref component.InvisibleWallActionEntity, component.InvisibleWallAction, uid);
            _actionsSystem.AddAction(uid, ref component.BalloonActionEntity, component.BalloonAction, uid); //ADT-Tweak
        }

        /// <summary>
        /// Creates an invisible wall in a free space after some checks.
        /// </summary>
        private void OnInvisibleWall(EntityUid uid, MimePowersComponent component, InvisibleWallActionEvent args)
        {
            if (!component.Enabled)
                return;

            if (_container.IsEntityOrParentInContainer(uid))
                return;

            var xform = Transform(uid);
            // Get the tile in front of the mime
            var offsetValue = xform.LocalRotation.ToWorldVec();
            var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan);
            var tile = coords.GetTileRef(EntityManager, _mapMan);
            if (tile == null)
                return;

            // Check if the tile is blocked by a wall or mob, and don't create the wall if so
            if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable | CollisionGroup.Opaque))
            {
                _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), uid, uid);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-popup", ("mime", uid)), uid);
            // Make sure we set the invisible wall to despawn properly
            Spawn(component.WallPrototype, _turf.GetTileCenter(tile.Value));
            // Handle args so cooldown works
            args.Handled = true;
        }

        //ADT-Tweak-Start
        private void OnSpawnBalloon(EntityUid uid, MimePowersComponent component, SpawnBaloonEvent args)
        {
            if (!component.Enabled)
                return;

            if (!TryComp<HandsComponent>(args.Performer, out var handsComponent))
                return;

            var randomPickPrototype = _random.Pick(component.BalloonPrototypes);
            var balloon = Spawn(randomPickPrototype, Transform(args.Performer).Coordinates);

            if (!_handsSystem.TryPickupAnyHand(args.Performer, balloon))
            {
                QueueDel(balloon);
                _popupSystem.PopupEntity(Loc.GetString("mime-baloon-fail"), args.Performer, args.Performer);
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

            _actionsSystem.StartUseDelay(component.BalloonActionEntity);
        }
        //ADT-Tweak-End

        private void OnBreakVowAlert(Entity<MimePowersComponent> ent, ref BreakVowAlertEvent args)
        {
            if (args.Handled)
                return;
            BreakVow(ent, ent);
            args.Handled = true;
        }

        private void OnRetakeVowAlert(Entity<MimePowersComponent> ent, ref RetakeVowAlertEvent args)
        {
            if (args.Handled)
                return;
            RetakeVow(ent, ent);
            args.Handled = true;
        }

        /// <summary>
        /// Break this mime's vow to not speak.
        /// </summary>
        public void BreakVow(EntityUid uid, MimePowersComponent? mimePowers = null)
        {
            if (!Resolve(uid, ref mimePowers))
                return;

            if (mimePowers.VowBroken)
                return;

            mimePowers.Enabled = false;
            mimePowers.VowBroken = true;
            mimePowers.VowRepentTime = _timing.CurTime + mimePowers.VowCooldown;
            RemComp<MutedComponent>(uid);
            if (mimePowers.PreventWriting)
                RemComp<BlockWritingComponent>(uid);
            _alertsSystem.ClearAlert(uid, mimePowers.VowAlert);
            _alertsSystem.ShowAlert(uid, mimePowers.VowBrokenAlert);
            _actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallActionEntity);
            _actionsSystem.RemoveAction(uid, mimePowers.BalloonActionEntity); //ADT-Tweak
        }

        /// <summary>
        /// Retake this mime's vow to not speak.
        /// </summary>
        public void RetakeVow(EntityUid uid, MimePowersComponent? mimePowers = null)
        {
            if (!Resolve(uid, ref mimePowers))
                return;

            if (!mimePowers.ReadyToRepent)
            {
                _popupSystem.PopupEntity(Loc.GetString("mime-not-ready-repent"), uid, uid);
                return;
            }

            mimePowers.Enabled = true;
            mimePowers.ReadyToRepent = false;
            mimePowers.VowBroken = false;
            AddComp<MutedComponent>(uid);
            if (mimePowers.PreventWriting)
            {
                EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
                illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
                Dirty(uid, illiterateComponent);
            }

            _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
            _alertsSystem.ShowAlert(uid, mimePowers.VowAlert);
            _actionsSystem.AddAction(uid, ref mimePowers.InvisibleWallActionEntity, mimePowers.InvisibleWallAction, uid);
            _actionsSystem.AddAction(uid, ref mimePowers.BalloonActionEntity, mimePowers.BalloonAction, uid); //ADT-Tweak
        }
    }
}
