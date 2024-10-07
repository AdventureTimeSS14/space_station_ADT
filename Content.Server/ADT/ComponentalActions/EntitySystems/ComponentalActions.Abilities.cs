using Content.Shared.ComponentalActions.Components;
using Content.Shared.ComponentalActions;
using Content.Shared.Inventory;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Server.Body.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Stealth.Components;
using Content.Server.Emp;
using Content.Shared.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Actions;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Content.Shared.Clothing;
using Content.Server.Electrocution;
using Content.Server.Lightning;
using Robust.Shared.Timing;
using Robust.Shared.Spawners;
using Content.Server.Spawners.EntitySystems;


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
    [Dependency] private readonly SpawnOnDespawnSystem _timeDespawnUid = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PointLightSystem _light = default!;

    private void InitializeCompAbilities()
    {
        SubscribeLocalEvent<TeleportActComponent, CompTeleportActionEvent>(OnTeleport);
        //SubscribeLocalEvent<ProjectileActComponent, CompProjectileActionEvent>(OnProjectile);
        SubscribeLocalEvent<HealActComponent, CompHealActionEvent>(OnHeal);
        SubscribeLocalEvent<JumpActComponent, CompJumpActionEvent>(OnJump);
        SubscribeLocalEvent<StasisHealActComponent, CompStasisHealActionEvent>(OnStasisHeal);
        SubscribeLocalEvent<InvisibilityActComponent, CompInvisibilityActionEvent>(OnInvisibility);
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
    // private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, ComponentalActionsSpawnData data)
    // {
    //     switch (data)
    //     {
    //         case TargetCasterPos:
    //             return new List<EntityCoordinates>(1) { casterXform.Coordinates };
    //         case TargetInFront:
    //             {
    //                 // This is shit but you get the idea.
    //                 var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

    //                 if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
    //                     return new List<EntityCoordinates>();

    //                 if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
    //                     return new List<EntityCoordinates>();

    //                 var tileIndex = tileReference.Value.GridIndices;
    //                 var coords = mapGrid.GridTileToLocal(tileIndex);
    //                 EntityCoordinates coordsPlus;
    //                 EntityCoordinates coordsMinus;

    //                 var dir = casterXform.LocalRotation.GetCardinalDir();
    //                 switch (dir)
    //                 {
    //                     case Direction.North:
    //                     case Direction.South:
    //                         {
    //                             coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
    //                             coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
    //                             return new List<EntityCoordinates>(3)
    //                     {
    //                         coords,
    //                         coordsPlus,
    //                         coordsMinus,
    //                     };
    //                         }
    //                     case Direction.East:
    //                     case Direction.West:
    //                         {
    //                             coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
    //                             coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
    //                             return new List<EntityCoordinates>(3)
    //                     {
    //                         coords,
    //                         coordsPlus,
    //                         coordsMinus,
    //                     };
    //                         }
    //                 }

    //                 return new List<EntityCoordinates>();
    //             }
    //         default:
    //             throw new ArgumentOutOfRangeException();
    //     }
    // }


    private void OnTeleport(EntityUid uid, TeleportActComponent component, CompTeleportActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);

        if (transform.MapID != _transformSystem.GetMapId(args.Target))
            return;

        _transformSystem.SetCoordinates(uid, args.Target);
        transform.AttachToGridOrMap();
        _audio.PlayPvs(component.BlinkSound, uid, AudioParams.Default.WithVolume(component.BlinkVolume));
        args.Handled = true;
    }

    // private void OnProjectile(EntityUid uid, ProjectileActComponent component, CompProjectileActionEvent args)
    // {
    //     if (args.Handled)
    //         return;

    //     args.Handled = true;

    //     var xform = Transform(uid);
    //     var userVelocity = _physics.GetMapLinearVelocity(uid);

    //     foreach (var pos in GetSpawnPositions(xform, component.Pos))
    //     {
    //         // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
    //         var mapPos = _transformSystem.ToMapCoordinates(args.Target);
    //         var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
    //             ? pos.WithEntityId(gridUid, EntityManager)
    //             : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

    //         var ent = Spawn(component.Prototype, spawnCoords);
    //         var direction = args.Target.ToMapPos(EntityManager, _transformSystem) -
    //                         spawnCoords.ToMapPos(EntityManager, _transformSystem);
    //         _gunSystem.ShootProjectile(ent, direction, userVelocity, uid, uid);
    //         _audio.PlayPvs(component.ShootSound, uid, AudioParams.Default.WithVolume(component.ShootVolume));
    //     }
    // }

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

        if (transform.MapID != _transformSystem.GetMapId(args.Target))
            return;

        _throwing.TryThrow(uid, args.Target, component.Strength);
        _audio.PlayPvs(component.Sound, uid, AudioParams.Default.WithVolume(component.Volume));
        args.Handled = true;
    }

    private void OnStasisHeal(EntityUid uid, StasisHealActComponent component, CompStasisHealActionEvent args)
    {
        if (args.Handled)
            return;

        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);
        var sprintSpeed = component.Active ? component.BaseSprintSpeed : component.SpeedModifier;
        var walkSpeed = component.Active ? component.BaseWalkSpeed : component.SpeedModifier;

        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration, movementSpeed);

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

    private void OnElectrionPulse(EntityUid uid, ElectrionPulseActComponent component, CompElectrionPulseActionEvent args)
    {
        if (args.Handled)
            return;
        _chat.TrySendInGameICMessage(uid, "щёлкает пальцами", InGameICChatType.Emote, ChatTransmitRange.Normal);

        if (!HasComp<TimedDespawnComponent>(uid))
        {
            var despawn = AddComp<TimedDespawnComponent>(uid);
            despawn.Lifetime = 1.5f;
            _audio.PlayPvs(component.IgniteSound, uid);
        }

        args.Handled = true;
    }
}
