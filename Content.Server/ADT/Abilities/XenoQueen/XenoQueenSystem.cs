using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Maps;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Magic.Events;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.ADT.Events;
using Robust.Shared.Network;
using Content.Shared.Magic;
using Content.Shared.FixedPoint;

namespace Content.Server.Abilities.XenoQueen
{
    public sealed class XenoQueenSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<XenoQueenComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<XenoQueenComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<XenoQueenComponent, InvisibleWallActionEvent>(OnCreateTurret);
            SubscribeLocalEvent<XenoQueenComponent, SpawnXenoQueenEvent>(OnWorldSpawn);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<XenoQueenComponent>(); 
            while (query.MoveNext(out var uid, out var component) && component.Regenetarion) // Костыль, но супер рабочий)
            {
                if (component.BloobCount >= component.MaxBloobCount) 
                {
                    component.Accumulator = 0f; 
                    continue;
                }

                component.Accumulator += frameTime; // 0.000001

                if (component.Accumulator <= component.RegenDelay)
                    continue;

                component.Accumulator -= component.RegenDelay; // component.Accumulator = 0f;
                if (component.BloobCount < component.MaxBloobCount) 
                {
                    ChangePowerAmount(uid, component.RegenBloobCount, component);
                }
            }
        }
        public void ChangePowerAmount(EntityUid uid, FixedPoint2 amount, XenoQueenComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.BloobCount + amount < 0)
                return;

            component.BloobCount += amount;
            //_alerts.ShowAlert(uid, _proto.Index(component.Alert), (short)Math.Clamp(Math.Round(component.Power.Float()), 0, 5));
        }
        private void OnMapInit(EntityUid uid, XenoQueenComponent component, MapInitEvent args)
        {
            _actionsSystem.AddAction(uid, ref component.XenoTurretActionEntity, component.XenoTurretAction, uid);
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoBurrower, "ActionSpawnMobXenoBurrower");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoDrone, "ActionSpawnMobXenoDrone");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoRunner, "ActionSpawnMobXenoRunner");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoSpitter, "ActionSpawnMobXenoSpitter");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoPraetorian, "ActionSpawnMobXenoPraetorian");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoRavager, "ActionSpawnMobXenoRavager");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoQueen, "ActionSpawnMobXenoQueen");
        }
        private void OnShutdown(EntityUid uid, XenoQueenComponent component, ComponentShutdown args)
        {
            _actionsSystem.RemoveAction(uid, component.XenoTurretActionEntity);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoBurrower);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoDrone);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoRunner);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoSpitter);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoPraetorian);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoRavager);
            _actionsSystem.RemoveAction(uid, component.ActionSpawnXenoQueen);
        }
        private void OnCreateTurret(EntityUid uid, XenoQueenComponent component, InvisibleWallActionEvent args)
        {
            if (!component.XenoCreatTurretEnabled)
                return;

            if (_container.IsEntityOrParentInContainer(uid))
                return;

            var xform = Transform(uid);
            // Get the tile in front of the Qeen
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
        // Spawn Tipo
        private void OnWorldSpawn(EntityUid uid, XenoQueenComponent component, SpawnXenoQueenEvent args) // SpawnXenoQueenEvent
        {
            if (component.BloobCount > args.Cost)
            {
                component.BloobCount -= args.Cost.Value;
                Spawn(args.Prototypes[0].PrototypeId, Transform(uid).Coordinates);
                Speak(args);
                args.Handled = true;
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("queen-no-bloob-count", ("CountBloob", args.Cost.GetValueOrDefault() - component.BloobCount)), uid);
            }
        }
        private void Speak(BaseActionEvent args)
        {
            if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
                return;

            var ev = new SpeakSpellEvent(args.Performer, speak.Speech);
            RaiseLocalEvent(ref ev);
        }
    }
}
