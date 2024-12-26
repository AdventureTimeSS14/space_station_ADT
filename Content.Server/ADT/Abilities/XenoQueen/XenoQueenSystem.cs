using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Maps;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Magic.Events;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Spawners;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Numerics;
using Content.Shared.Storage;
using Robust.Shared.Network;
using Content.Shared.Magic;

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
            SubscribeLocalEvent<XenoQueenComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<XenoQueenComponent, InvisibleWallActionEvent>(OnCreateTurret);
            SubscribeLocalEvent<SpawnXenoQueenEvent> (OnWorldSpawn);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }
        private void OnComponentInit(EntityUid uid, XenoQueenComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, ref component.XenoTurretActionEntity, component.XenoTurretAction, uid);
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoBurrower, "ActionSpawnMobXenoBurrower");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoDrone, "ActionSpawnMobXenoDrone");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoRunner, "ActionSpawnMobXenoRunner");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoSpitter, "ActionSpawnMobXenoSpitter");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoPraetorian, "ActionSpawnMobXenoPraetorian");
            _actionsSystem.AddAction(uid, ref component.ActionSpawnXenoRavager, "ActionSpawnMobXenoRavager");
            //_actionsSystem.AddAction(uid, ref component.ActionSpawnXenoQueen, "ActionSpawnMobXenoQueen");
        }
        private void OnCreateTurret(EntityUid uid, XenoQueenComponent component, InvisibleWallActionEvent args)
        {
            if (!component.Enabled)
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
        private void OnWorldSpawn(SpawnXenoQueenEvent args)
        {
            if (args.Handled || !PassesSpellPrerequisites(args.Action, args.Performer))
                return;

            var targetMapCoords = args.Target;

            WorldSpawnSpellHelper(args.Prototypes, targetMapCoords, args.Performer, args.Lifetime, args.Offset);
            Speak(args);
            args.Handled = true;
        }
        // Help
        private void WorldSpawnSpellHelper(List<EntitySpawnEntry> entityEntries, EntityCoordinates entityCoords, EntityUid performer, float? lifetime, Vector2 offsetVector2)
        {
            var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, _random);

            var offsetCoords = entityCoords;
            foreach (var proto in getProtos)
            {
                SpawnSpellHelper(proto, offsetCoords, performer, lifetime);
                offsetCoords = offsetCoords.Offset(offsetVector2);
            }
        }
        // Help 2
        private void SpawnSpellHelper(string? proto, EntityCoordinates position, EntityUid performer, float? lifetime = null, bool preventCollide = false)
        {
            if (!_net.IsServer)
                return;

            var ent = Spawn(proto, position.SnapToGrid(EntityManager, _mapManager));

            if (lifetime != null)
            {
                var comp = EnsureComp<TimedDespawnComponent>(ent);
                comp.Lifetime = lifetime.Value;
            }

            if (preventCollide)
            {
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = performer;
            }
        }
        //
        private bool PassesSpellPrerequisites(EntityUid spell, EntityUid performer)
        {
            var ev = new BeforeCastSpellEvent(performer);
            RaiseLocalEvent(spell, ref ev);
            return !ev.Cancelled;
        }
        //
        private void Speak(BaseActionEvent args)
        {
            if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
                return;

            var ev = new SpeakSpellEvent(args.Performer, speak.Speech);
            RaiseLocalEvent(ref ev);
        }
    }
}
