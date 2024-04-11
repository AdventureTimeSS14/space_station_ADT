using Content.Shared.ComponentalActions.Components;
using Content.Shared.ComponentalActions;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Server.Body.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.IdentityManagement;
using Robust.Shared.Audio.Systems;
using Content.Shared.Stealth.Components;
using Content.Server.Emp;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Server.Forensics;
using Content.Shared.FixedPoint;
using Content.Server.Store.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Server.Destructible;
using Content.Shared.Polymorph;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Doors;
using Robust.Shared.Audio;
using System.Numerics;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Magic.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Content.Server.Electrocution;
using Content.Server.Lightning;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Server.Anomaly.Effects;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.ComponentalActions.EntitySystems;

public sealed partial class ComponentalActionsSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    private void InitializeCompAbilities()
    {
        SubscribeLocalEvent<TeleportActComponent, CompTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<ProjectileActComponent, CompProjectileActionEvent>(OnProjectile);
        SubscribeLocalEvent<HealActComponent, CompHealActionEvent>(OnHeal);
        SubscribeLocalEvent<JumpActComponent, CompJumpActionEvent>(OnJump);
        SubscribeLocalEvent<StasisHealActComponent, CompStasisHealActionEvent>(OnStasisHeal);
        SubscribeLocalEvent<InvisibilityActComponent, CompInvisibilityActionEvent>(OnInvisibility);
        SubscribeLocalEvent<LevitationActComponent, CompGravitationActionEvent>(OnMagGravity);
        SubscribeLocalEvent<ElectrionPulseActComponent, CompElectrionPulseActionEvent>(OnElectrionPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StasisHealActComponent>();
        while (query.MoveNext(out var uid, out var stasis))
        {
            if (stasis.Active)
            {
                var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), stasis.RegenerateBruteHealAmount);
                var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), stasis.RegenerateBurnHealAmount);
                var damage_airloss = new DamageSpecifier(_proto.Index(AirlossDamageGroup), stasis.RegenerateBurnHealAmount);
                var damage_toxin = new DamageSpecifier(_proto.Index(ToxinDamageGroup), stasis.RegenerateBurnHealAmount);
                var damage_genetic = new DamageSpecifier(_proto.Index(GeneticDamageGroup), stasis.RegenerateBurnHealAmount);
                _damageableSystem.TryChangeDamage(uid, damage_brute);
                _damageableSystem.TryChangeDamage(uid, damage_burn);
                _damageableSystem.TryChangeDamage(uid, damage_airloss);
                _damageableSystem.TryChangeDamage(uid, damage_toxin);
                _damageableSystem.TryChangeDamage(uid, damage_genetic);
                _bloodstreamSystem.TryModifyBloodLevel(uid, stasis.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
                _bloodstreamSystem.TryModifyBleedAmount(uid, stasis.RegenerateBleedReduceAmount);
            }
        }
    }
    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, ComponentalActionsSpawnData data)
    {
        switch (data)
        {
            case TargetCasterPos:
                return new List<EntityCoordinates>(1) { casterXform.Coordinates };
            case TargetInFront:
                {
                    // This is shit but you get the idea.
                    var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                    if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
                        return new List<EntityCoordinates>();

                    if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                        return new List<EntityCoordinates>();

                    var tileIndex = tileReference.Value.GridIndices;
                    var coords = mapGrid.GridTileToLocal(tileIndex);
                    EntityCoordinates coordsPlus;
                    EntityCoordinates coordsMinus;

                    var dir = casterXform.LocalRotation.GetCardinalDir();
                    switch (dir)
                    {
                        case Direction.North:
                        case Direction.South:
                            {
                                coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
                                coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
                                return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                            }
                        case Direction.East:
                        case Direction.West:
                            {
                                coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
                                coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
                                return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                            }
                    }

                    return new List<EntityCoordinates>();
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    private void OnTeleport(EntityUid uid, TeleportActComponent component, CompTeleportActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(uid, args.Target);
        transform.AttachToGridOrMap();
        _audio.PlayPvs(component.BlinkSound, uid, AudioParams.Default.WithVolume(component.BlinkVolume));
        args.Handled = true;
    }

    private void OnProjectile(EntityUid uid, ProjectileActComponent component, CompProjectileActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var xform = Transform(uid);
        var userVelocity = _physics.GetMapLinearVelocity(uid);

        foreach (var pos in GetSpawnPositions(xform, component.Pos))
        {
            // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
            var mapPos = pos.ToMap(EntityManager);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var ent = Spawn(component.Prototype, spawnCoords);
            var direction = args.Target.ToMapPos(EntityManager, _transformSystem) -
                            spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, uid, uid);
            _audio.PlayPvs(component.ShootSound, uid, AudioParams.Default.WithVolume(component.ShootVolume));
        }
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    public ProtoId<DamageGroupPrototype> AirlossDamageGroup = "Airloss";
    public ProtoId<DamageGroupPrototype> ToxinDamageGroup = "Toxin";
    public ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";

    private void OnHeal(EntityUid uid, HealActComponent component, CompHealActionEvent args)
    {
        if (args.Handled)
            return;

        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), component.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(uid, damage_brute);
        _damageableSystem.TryChangeDamage(uid, damage_burn);
        _bloodstreamSystem.TryModifyBloodLevel(uid, component.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
        _bloodstreamSystem.TryModifyBleedAmount(uid, component.RegenerateBleedReduceAmount);
        _audioSystem.PlayPvs(component.HealSound, uid, AudioParams.Default.WithVolume(component.HealVolume));

        args.Handled = true;
    }

    private void OnJump(EntityUid uid, JumpActComponent component, CompJumpActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _throwing.TryThrow(uid, args.Target, component.Strength);
        _audio.PlayPvs(component.Sound, uid, AudioParams.Default.WithVolume(component.Volume));
        args.Handled = true;
    }

    private void OnStasisHeal(EntityUid uid, StasisHealActComponent component, CompStasisHealActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.Active)
        {
            var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
            var sprintSpeed = component.BaseSprintSpeed;
            var walkSpeed = component.BaseWalkSpeed;
            _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);
        }

        if (!component.Active)
        {
            var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
            var sprintSpeed = component.SpeedModifier;
            var walkSpeed = component.SpeedModifier;
            _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);
        }

        component.Active = !component.Active;

        args.Handled = true;
    }

    private void OnInvisibility(EntityUid uid, InvisibilityActComponent component, CompInvisibilityActionEvent args)
    {
        if (args.Handled)
            return;

        var stealth = EnsureComp<StealthComponent>(uid); // cant remove the armor
        var stealthonmove = EnsureComp<StealthOnMoveComponent>(uid); // cant remove the armor

        var message = Loc.GetString(!component.Active ? "changeling-chameleon-toggle-on" : "changeling-chameleon-toggle-off");
        _popup.PopupEntity(message, uid, uid);

        if (!component.Active)
        {
            stealthonmove.PassiveVisibilityRate = component.PassiveVisibilityRate;
            stealthonmove.MovementVisibilityRate = component.MovementVisibilityRate;
            stealth.MinVisibility = component.MinVisibility;
            stealth.MaxVisibility = component.MaxVisibility;
        }
        else
        {
            RemCompDeferred(uid, stealth);
            RemCompDeferred(uid, stealthonmove);
        }

        component.Active = !component.Active;

        args.Handled = true;
    }

    private void OnMagGravity(EntityUid uid, LevitationActComponent component, CompGravitationActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleLevitation(uid, component);
    }
    private void ToggleLevitation(EntityUid uid, LevitationActComponent component)
    {
        component.Active = !component.Active;
        if (TryComp(uid, out MovedByPressureComponent? movedByPressure))
        {
            movedByPressure.Enabled = !component.Active;
            //_sharedActions.SetToggled(component.ActionEntity, component.Active);
        }

        if (component.Active)
        {
            _alerts.ShowAlert(uid, AlertType.ADTLevitation);
            AddComp<MovementIgnoreGravityComponent>(uid);
            var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
            var sprintSpeed = component.SpeedModifier;
            var walkSpeed = component.SpeedModifier;
            _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);

        }
        else
        {
            _alerts.ClearAlert(uid, AlertType.ADTLevitation);
            RemComp<MovementIgnoreGravityComponent>(uid);
            var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
            var sprintSpeed = component.BaseSprintSpeed;
            var walkSpeed = component.BaseWalkSpeed;
            _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);
        }
    }

    private void OnElectrionPulse(EntityUid uid, Entity<ElectrionPulseActComponent> component, CompElectrionPulseActionEvent args)
    {
        if (args.Handled)
            return;
        OnElectrionPulseAction(uid, component);
        args.Handled = true;
    }

    private void OnElectrionPulseAction(EntityUid uid, ElectrionPulseActComponent component)//, FireStarterActionEvent args)
    {
        if (_container.IsEntityOrParentInContainer(uid))
            return;

        var xform = Transform(uid);
        var ignitionRadius = component.IgnitionRadius;
        IgniteNearby(uid, xform.Coordinates, component.Severity, ignitionRadius, component);
        //_audio.PlayPvs(component.IgniteSound, uid);

        //args.Handled = true;
    }

    /// <summary>
    /// Ignites flammable objects within range.
    /// </summary>
    public void IgniteNearby(EntityUid uid, EntityCoordinates coordinates, float severity, float radius, ElectrionPulseActComponent component)
    {
        //_flammables.Clear();
        //_lookup.GetEntitiesInRange(coordinates, radius, _electrocution);
        var range = component.MaxElectrocuteRange * component.Severity; //component.Stability;
        var damage = (int) (component.MaxElectrocuteDamage * component.Severity);
        var duration = component.MaxElectrocuteDuration * component.Severity;

        foreach (var flammable in _electrocutio)
        {
            var ent = flammable.Owner;
            if (ent != uid)
            {
                var stackAmount = 2 + (int) (severity / 0.15f);
                //_flammable.AdjustFireStacks(ent, stackAmount, flammable);
                //_flammable.Ignite(ent, uid, flammable);
                _electrocution.TryDoElectrocution(ent, uid, damage, duration, true, statusEffects: comp, ignoreInsulation: true);
            }

        }
    }

}
