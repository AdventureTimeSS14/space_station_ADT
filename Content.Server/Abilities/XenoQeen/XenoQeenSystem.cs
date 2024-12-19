using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Abilities.XenoQeen
{
    public sealed class XenoQeenSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<XenoQeenComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<XenoQeenComponent, InvisibleWallActionEvent>(OnInvisibleWall);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }
        private void OnComponentInit(EntityUid uid, XenoQeenComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, ref component.XenoTurretActionEntity, component.XenoTurretAction, uid);
        }
        private void OnInvisibleWall(EntityUid uid, XenoQeenComponent component, InvisibleWallActionEvent args)
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
                _popupSystem.PopupEntity(Loc.GetString("create-turret-failed"), uid, uid);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("create-turret"), uid);
            // Make sure we set the invisible wall to despawn properly
            Spawn(component.XenoTurret, _turf.GetTileCenter(tile.Value));
            // Handle args so cooldown works
            args.Handled = true;
        }

    }
}
