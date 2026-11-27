using System.Numerics;
using Content.Shared.Vehicle.Components; // ADT-Tweak
using Content.Server.Cargo.Systems;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Cargo;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Containers;
using Content.Server.Body.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.PowerCell;
using Content.Shared._RMC14.Weapons.Ranged.Flamer; // ADT-Tweak
using Content.Shared.ADT.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedRMCFlamerSystem _rmcFlamer = default!; // ADT-Tweak
    [Dependency] private INetConfigurationManager _netConfig = default!;

// ADT-Tweak Start
//     private const float DamagePitchVariation = 0.05f;
// ADT-Tweak End
    private bool PredictsHitscans(ICommonSession session)
        => _netConfig.GetClientCVar(session.Channel, ADTCCVars.HitscanPrediction);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BallisticAmmoProviderComponent, PriceCalculationEvent>(OnBallisticPrice);
    }

    private void OnBallisticPrice(Entity<BallisticAmmoProviderComponent> ent, ref PriceCalculationEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Proto) || ent.Comp.UnspawnedCount == 0)
            return;

        if (!ProtoMan.TryIndex<EntityPrototype>(ent.Comp.Proto, out var proto))
        {
            Log.Error($"Unable to find fill prototype for price on {ent.Comp.Proto} on {ToPrettyString(ent)}");
            return;
        }

        // Probably good enough for most.
        var price = _pricing.GetEstimatedPrice(proto);
        args.Price += price * ent.Comp.UnspawnedCount;
    }

    public override void Shoot(Entity<GunComponent> gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, out bool userImpulse, EntityUid? user = null, bool throwItems = false)
    {
        userImpulse = true;

        if (user != null)
        {
            var selfEvent = new SelfBeforeGunShotEvent(user.Value, gun, ammo);
            RaiseLocalEvent(user.Value, selfEvent);
            if (selfEvent.Cancelled)
            {
                userImpulse = false;
                return;
            }
        }

        var fromMap = TransformSystem.ToMapCoordinates(fromCoordinates);
        var toMap = TransformSystem.ToMapCoordinates(toCoordinates).Position;
// ADT-Tweak Start
//         var mapDirection = toMap - fromMap.Position;
//         var mapAngle = mapDirection.ToAngle();
//         var angle = GetRecoilAngle(Timing.CurTime, gun, mapDirection.ToAngle());
//
//         // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
// ADT-Tweak End
        var mapDirection = GetShotMapDirection(gun, fromMap.Position, toMap);
        var angle = mapDirection.ToAngle();
        var mapAngle = angle;
        var fromEnt = Maps.TryFindGridAt(fromMap, out var gridUid, out _)
            ? TransformSystem.WithEntityId(fromCoordinates, gridUid)
            : new EntityCoordinates(_map.GetMapOrInvalid(fromMap.MapId), fromMap.Position);

// ADT-Tweak Start
//         // Update shot based on the recoil
//         toMap = fromMap.Position + angle.ToVec() * mapDirection.Length();
//         mapDirection = toMap - fromMap.Position;
// ADT-Tweak End
        var gunVelocity = Physics.GetMapLinearVelocity(fromEnt);

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);
        var shotProjectiles = new List<EntityUid>(ammo.Count);
        var ammoIndex = -1;

        foreach (var (ent, shootable) in ammo)
        {
            ammoIndex++;
            // pneumatic cannon doesn't shoot bullets it just throws them, ignore ammo handling
            if (throwItems && ent != null)
            {
                ShootOrThrow(ent.Value, mapDirection, gunVelocity, gun, user);
                continue;
            }

            // TODO: Clean this up in a gun refactor at some point - too much copy pasting
            switch (shootable)
            {
                // Cartridge shoots something else
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        var uid = Spawn(cartridge.Prototype, fromEnt);
// ADT-Tweak Start
//                         CreateAndFireProjectiles(uid, cartridge);
// ADT-Tweak End

                        // Kinetic cartridge ammo may spawn hitscan bullet entities (Starlight Shooting 2.0 port).
                        if (HasComp<HitscanAmmoComponent>(uid))
                            CreateAndFireHitscan(uid);
                        else
                            CreateAndFireProjectiles(uid, cartridge);

                        RaiseLocalEvent(ent!.Value, new AmmoShotEvent()
                        {
                            FiredProjectiles = shotProjectiles,
                        });

                        SetCartridgeSpent(ent.Value, cartridge, true);

                        if (cartridge.DeleteOnSpawn)
                            Del(ent.Value);
                    }
                    else
                    {
                        userImpulse = false;
                        // ADT Mech start
                        if (TryComp<MechComponent>(user, out var cmech))
                        {
                            Audio.PlayPredicted(gun.Comp.SoundEmpty, gun.Owner, CompOrNull<VehicleComponent>(user)?.Operator);
                        }
                        else
                            Audio.PlayPredicted(gun.Comp.SoundEmpty, gun.Owner, user);
                        // ADT Mech end
                    }

                    // Something like ballistic might want to leave it in the container still
// ADT-Tweak Start
//                     if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value))
//                         EjectCartridge(ent.Value, angle);
// ADT-Tweak End
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value) && !gun.Comp.Pump)
                        EjectCartridge(ent.Value, angle, user: user);

                    Dirty(ent!.Value, cartridge);
                    break;
                // Ammo shoots itself
                case AmmoComponent newAmmo:
                    if (ent == null)
                        break;
                    CreateAndFireProjectiles(ent.Value, newAmmo);

                    break;
                case HitscanAmmoComponent:
                    if (ent == null)
                        break;

// ADT-Tweak Start
//                     var hitscanEv = new HitscanTraceEvent
//                     {
//                         FromCoordinates = fromCoordinates,
//                         ShotDirection = mapDirection.Normalized(),
//                         Gun = gun,
//                         Shooter = user,
//                         Target = gun.Comp.Target,
//                     };
//                     RaiseLocalEvent(ent.Value, ref hitscanEv);
//
//                     Del(ent);
//
//                     Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
// ADT-Tweak End
                    CreateAndFireHitscan(ent.Value);
                    break;
                // ADT-Tweak-start
                case RMCFlamerAmmoProviderComponent flamer:
                    if (ent == null)
                        break;

                    _rmcFlamer.ShootFlamer((ent.Value, flamer), gun, user, fromCoordinates, toCoordinates);
                    break;
                // ADT-Tweak-end
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        RaiseLocalEvent(gun, new AmmoShotEvent()
        {
            FiredProjectiles = shotProjectiles,
        });

        void CreateAndFireProjectiles(EntityUid ammoEnt, AmmoComponent ammoComp)
        {
// ADT-Tweak Start
//             if (TryComp<ProjectileSpreadComponent>(ammoEnt, out var ammoSpreadComp))
// ADT-Tweak End
            if (TryComp<ProjectileSpreadComponent>(ammoEnt, out var ammoSpreadComp) && ammoSpreadComp.Count > 1)
            {
// ADT-Tweak Start
//                 var spreadEvent = new GunGetAmmoSpreadEvent(ammoSpreadComp.Spread);
//                 RaiseLocalEvent(gun, ref spreadEvent);
//
//                 var angles = LinearSpread(mapAngle - spreadEvent.Spread / 2,
//                     mapAngle + spreadEvent.Spread / 2, ammoSpreadComp.Count);
// ADT-Tweak End
                var angles = GetPelletAngles(gun, ammoSpreadComp, mapAngle, ammoIndex);

                ShootOrThrow(ammoEnt, angles[0].ToVec(), gunVelocity, gun, user);
                shotProjectiles.Add(ammoEnt);

                for (var i = 1; i < ammoSpreadComp.Count; i++)
                {
                    var newuid = Spawn(ammoSpreadComp.Proto, fromEnt);
                    ShootOrThrow(newuid, angles[i].ToVec(), gunVelocity, gun, user);
                    shotProjectiles.Add(newuid);
                }
            }
            else
            {
                ShootOrThrow(ammoEnt, mapDirection, gunVelocity, gun, user);
                shotProjectiles.Add(ammoEnt);
            }

            MuzzleFlash(gun.Owner, ammoComp, mapDirection.ToAngle(), user);
            PlayGunshot();
        }

        void CreateAndFireHitscan(EntityUid ammoEnt)
        {
            if (TryComp<ProjectileSpreadComponent>(ammoEnt, out var ammoSpreadComp) && ammoSpreadComp.Count > 1)
            {
                var originalSpread = ammoSpreadComp.Spread;
                ammoSpreadComp.Spread = (originalSpread + gun.Comp.Spread) / 2;
                Angle[] angles;
                try
                {
                    angles = GetPelletAngles(gun, ammoSpreadComp, mapAngle, ammoIndex);
                }
                finally
                {
                    ammoSpreadComp.Spread = originalSpread;
                }

                FireHitscanTrace(ammoEnt, angles[0].ToVec(), 0);

                for (var i = 1; i < ammoSpreadComp.Count; i++)
                {
                    var newuid = Spawn(ammoSpreadComp.Proto, fromEnt);
                    FireHitscanTrace(newuid, angles[i].ToVec(), i);
                }
            }
            else
            {
                FireHitscanTrace(ammoEnt, mapDirection, 0);
            }

            PlayGunshot();
        }

        void FireHitscanTrace(EntityUid hitscanEnt, Vector2 direction, int pelletIndex)
        {
            var dir = direction == Vector2.Zero ? mapDirection : direction;
            if (dir == Vector2.Zero)
            {
                Del(hitscanEnt);
                return;
            }

            var hitscanEv = new HitscanTraceEvent
            {
                FromCoordinates = fromCoordinates,
                ToCoordinates = toCoordinates,
                ShotDirection = dir.Normalized(),
                Gun = gun,
                Shooter = user,
                Target = gun.Comp.Target,
                PredictionSeed = GetHitscanSeed(gun, ammoIndex, pelletIndex),
            };
            RaiseLocalEvent(hitscanEnt, ref hitscanEv);
            Del(hitscanEnt);
        }

        void PlayGunshot()
        {
            if (TryComp<MechComponent>(user, out var mech)) // ADT Mech gun fix
                Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun.Owner, CompOrNull<VehicleComponent>(user)?.Operator);
            else
                Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun.Owner, user);
        }
    }

// ADT-Tweak Start
//     private void ShootOrThrow(EntityUid uid, Vector2 mapDirection, Vector2 gunVelocity, Entity<GunComponent> gun, EntityUid? user)
// ADT-Tweak End
    private void ShootOrThrow(EntityUid uid, Vector2 mapDirection, Vector2 gunVelocity, Entity<GunComponent> gun, EntityUid? user, int? predictionSeed = null) // Starlight-edit
    {
        if (gun.Comp.Target is { } target && !TerminatingOrDeleted(target))
        {
            var targeted = EnsureComp<TargetedProjectileComponent>(uid);
            targeted.Target = target;
            // ADT-Crawling-Abuse-Tweak start
            targeted.TargetCoords = null;
            if (gun.Comp.ShootCoordinates is { } shootCoords)
            {
                var shootMap = TransformSystem.ToMapCoordinates(shootCoords);
                if (_map.TryGetMap(shootMap.MapId, out var shootMapUid))
                    targeted.TargetCoords = new EntityCoordinates(shootMapUid.Value, shootMap.Position);
            }
            // ADT-Crawling-Abuse-Tweak end
            Dirty(uid, targeted);
        }

        // Do a throw
        if (!HasComp<ProjectileComponent>(uid))
        {
            RemoveShootable(uid);
            // TODO: Someone can probably yeet this a billion miles so need to pre-validate input somewhere up the call stack.
            ThrowingSystem.TryThrow(uid, mapDirection, gun.Comp.ProjectileSpeedModified, user);
            return;
        }

        // ADT-Tweak-Start
        if (mapDirection.LengthSquared() > 0f)
            TransformSystem.SetWorldPosition(uid, TransformSystem.GetWorldPosition(uid) + mapDirection.Normalized() * 0.35f);
        // ADT-Tweak-End
        ShootProjectile(uid, mapDirection, gunVelocity, gun, user, gun.Comp.ProjectileSpeedModified);
    }

// ADT-Tweak Start
//     /// <summary>
//     /// Gets a linear spread of angles between start and end.
//     /// </summary>
//     /// <param name="start">Start angle in degrees</param>
//     /// <param name="end">End angle in degrees</param>
//     /// <param name="intervals">How many shots there are</param>
//     private Angle[] LinearSpread(Angle start, Angle end, int intervals)
//     {
//         var angles = new Angle[intervals];
//         DebugTools.Assert(intervals > 1);
//
//         for (var i = 0; i <= intervals - 1; i++)
//         {
//             angles[i] = new Angle(start + (end - start) * i / (intervals - 1));
//         }
//
//         return angles;
//     }
//
//     private Angle GetRecoilAngle(TimeSpan curTime, GunComponent component, Angle direction)
//     {
//         var timeSinceLastFire = (curTime - component.LastFire).TotalSeconds;
//         var newTheta = MathHelper.Clamp(component.CurrentAngle.Theta + component.AngleIncreaseModified.Theta - component.AngleDecayModified.Theta * timeSinceLastFire, component.MinAngleModified.Theta, component.MaxAngleModified.Theta);
//         component.CurrentAngle = new Angle(newTheta);
//         component.LastFire = component.NextFire;
//
//         // Convert it so angle can go either side.
//         var random = Random.NextFloat(-0.5f, 0.5f);
//         var spread = component.CurrentAngle.Theta * random;
//         var angle = new Angle(direction.Theta + component.CurrentAngle.Theta * random);
//         DebugTools.Assert(spread <= component.MaxAngleModified.Theta);
//         return angle;
//     }
//
// ADT-Tweak End
    protected override void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? user = null)
    {
        var filter = Filter.Pvs(gunUid, entityManager: EntityManager);

        if (TryComp<ActorComponent>(user, out var actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(message, filter);
    }

    public override void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound)
        => PlayImpactSound(otherEntity, modifiedDamage, weaponSound, forceWeaponSound, null);

    public override void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound, EntityUid? shooter)
    {
        DebugTools.Assert(!Deleted(otherEntity), "Impact sound entity was deleted");

// ADT-Tweak Start
//         // Like projectiles and melee,
//         // 1. Entity specific sound
//         // 2. Ammo's sound
//         // 3. Nothing
//         var playedSound = false;
// ADT-Tweak End
        var sound = GetImpactSound(otherEntity, modifiedDamage, weaponSound, forceWeaponSound, out var variation);
        if (sound == null)
            return;

// ADT-Tweak Start
//         if (!forceWeaponSound && modifiedDamage != null && modifiedDamage.GetTotal() > 0 && TryComp<RangedDamageSoundComponent>(otherEntity, out var rangedSound))
//         {
//             var type = SharedMeleeWeaponSystem.GetHighestDamageSound(modifiedDamage, ProtoMan);
// ADT-Tweak End
        var filter = Filter.Pvs(otherEntity, entityManager: EntityManager);

// ADT-Tweak Start
//             if (type != null && rangedSound.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
//             {
//                 var damageSoundTypeParams = damageSoundType?.Params ?? AudioParams.Default;
//                 damageSoundTypeParams = damageSoundTypeParams.WithVariation(DamagePitchVariation);
//                 Audio.PlayPvs(damageSoundType, otherEntity, damageSoundTypeParams);
//                 playedSound = true;
//             }
//             else if (type != null && rangedSound.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
//             {
//                 var damageSoundGroupParams = damageSoundGroup?.Params ?? AudioParams.Default;
//                 damageSoundGroupParams = damageSoundGroupParams.WithVariation(DamagePitchVariation);
//                 Audio.PlayPvs(damageSoundGroup, otherEntity, damageSoundGroupParams);
//                 playedSound = true;
//             }
//         }
// ADT-Tweak End
        if (TryComp<ActorComponent>(shooter, out var actor) && PredictsHitscans(actor.PlayerSession))
            filter.RemovePlayer(actor.PlayerSession);

// ADT-Tweak Start
//         if (!playedSound && weaponSound != null)
//         {
//             Audio.PlayPvs(weaponSound, otherEntity);
//         }
// ADT-Tweak End
        Audio.PlayEntity(sound, filter, otherEntity, true,
            variation ? AudioParams.Default.WithVariation(DamagePitchVariation) : null);
    }
}
