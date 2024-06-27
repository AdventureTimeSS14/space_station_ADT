using Content.Shared.Wizard;
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
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;


namespace Content.Server.Wizard.EntitySystems;

public sealed partial class WizardSystem
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

    private void InitializeWizardAbilities()
    {
        base.Initialize();

        SubscribeLocalEvent<WizardTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<WizardProjectileActionEvent>(OnProjectile);
        SubscribeLocalEvent<WizardHealActionEvent>(OnHeal);
    }

    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, WizardSpawnData data)
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


    private void OnTeleport(WizardTeleportActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(args.Performer, args.Target);
        transform.AttachToGridOrMap();
        _audio.PlayPvs(args.BlinkSound, args.Performer, AudioParams.Default.WithVolume(args.BlinkVolume));
        args.Handled = true;
    }

    private void OnProjectile(WizardProjectileActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var xform = Transform(args.Performer);
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer);

        foreach (var pos in GetSpawnPositions(xform, args.Pos))
        {
            // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
            var mapPos = pos.ToMap(EntityManager);
            var spawnCoords = _mapManager.TryFindGridAt(mapPos, out var gridUid, out _)
                ? pos.WithEntityId(gridUid, EntityManager)
                : new(_mapManager.GetMapEntityId(mapPos.MapId), mapPos.Position);

            var ent = Spawn(args.Prototype, spawnCoords);
            var direction = args.Target.ToMapPos(EntityManager, _transformSystem) -
                            spawnCoords.ToMapPos(EntityManager, _transformSystem);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, args.Performer, args.Performer);
            _audio.PlayPvs(args.ShootSound, args.Performer, AudioParams.Default.WithVolume(args.ShootVolume));
        }
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";

    private void OnHeal(WizardHealActionEvent args)
    {
        if (args.Handled)
            return;

        var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), args.RegenerateBruteHealAmount);
        var damage_burn = new DamageSpecifier(_proto.Index(BurnDamageGroup), args.RegenerateBurnHealAmount);
        _damageableSystem.TryChangeDamage(args.Performer, damage_brute);
        _damageableSystem.TryChangeDamage(args.Performer, damage_burn);
        _bloodstreamSystem.TryModifyBloodLevel(args.Performer, args.RegenerateBloodVolumeHealAmount); // give back blood and remove bleeding
        _bloodstreamSystem.TryModifyBleedAmount(args.Performer, args.RegenerateBleedReduceAmount);
        _audioSystem.PlayPvs(args.HealSound, args.Performer, AudioParams.Default.WithVolume(args.HealVolume));

        args.Handled = true;
    }

}

