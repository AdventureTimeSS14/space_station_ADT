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
using Robust.Shared.Map.Components;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Server.Player;
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
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        //ADT-Tweak-End

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);

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
            var directionPos = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan); //ADT-Tweak
            var tile = directionPos.GetTileRef(EntityManager, _mapMan); //ADT-Tweak
            if (tile == null)
                return;

            //ADT-Tweak-Start
            if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGrid))
                return;

            var tileIndex = tile.Value.GridIndices;
            var wallPositions = new List<EntityCoordinates>();

            var dir = xform.LocalRotation.GetCardinalDir();
            switch (dir)
            {
                case Direction.North:
                case Direction.South:
                {
                    for (int i = -(component.WallCount / 2); i <= component.WallCount / 2; i++)
                    {
                        var coords = _mapSystem.GridTileToLocal(xform.GridUid.Value, mapGrid, tileIndex + (i, 0));
                        wallPositions.Add(coords);
                    }
                    break;
                }
                case Direction.East:
                case Direction.West:
                {
                    for (int i = -(component.WallCount / 2); i <= component.WallCount / 2; i++)
                    {
                        var coords = _mapSystem.GridTileToLocal(xform.GridUid.Value, mapGrid, tileIndex + (0, i));
                        wallPositions.Add(coords);
                    }
                    break;
                }
            }

            if (wallPositions.Count == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), uid, uid);
                return;
            }
            //ADT-Tweak-End

            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-popup", ("mime", uid)), uid);
            //ADT-Tweak-Start
            var message = Loc.GetString("mime-invisible-wall-emote", ("entity", uid));
            var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
                ("entityName", Name(uid)),
                ("message", message));

            if (_playerManager.TryGetSessionByEntity(uid, out var session))
            {
                _chatManager.ChatMessageToOne(ChatChannel.Emotes, message, wrappedMessage, uid, false, session.Channel);
            }

            foreach (var wallCoords in wallPositions)
            {
                Spawn(component.WallPrototype, wallCoords);
            }
            //ADT-Tweak-End

            args.Handled = true;
        }

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
        }
    }
}
