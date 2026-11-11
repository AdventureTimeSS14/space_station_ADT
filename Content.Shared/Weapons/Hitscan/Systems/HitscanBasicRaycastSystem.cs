using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.ADT.Weapons.Hitscan.Components;
using Content.Shared.ADT.Weapons.Hitscan.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicRaycastSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ISharedAdminLogManager _log = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _rand = default!; // ADT hitscan #3142

    [Dependency] private EntityQuery<HitscanBasicVisualsComponent> _visualsQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicRaycastComponent, HitscanTraceEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<HitscanBasicRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        // Adventure BSA: unlimited pierce along the ray (legacy sprite list)
        if (HasComp<HitscanUnlimitedPierceComponent>(ent.Owner))
        {
            FireUnlimitedPierce(ent, ref args);
            return;
        }

        FireSingleHit(ent, ref args);
    }

    private void FireUnlimitedPierce(Entity<HitscanBasicRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        var shooter = args.Shooter ?? args.Gun;
        var ignored = args.IgnoredEntity;
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);
        var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) ent.Comp.CollisionMask);
// ADT-Tweak Start
//         var rayCastResults = _physics.IntersectRay(mapCords.MapId, ray, ent.Comp.MaxDistance, shooter, false);
// ADT-Tweak End
        var rayCastResults = _physics.IntersectRayWithPredicate(
            mapCords.MapId,
            ray,
            ent.Comp.MaxDistance,
            uid => uid == shooter || uid == ignored,
            false);

// ADT-Tweak Start
//         // ADT-Tweak BSA start
//         if (HasComp<HitscanPierceComponent>(ent.Owner))
// ADT-Tweak End
        FireLegacyEffects(args.FromCoordinates, ent.Comp.MaxDistance, args.ShotDirection.ToAngle(), ent.Owner);

        foreach (var hit in rayCastResults)
        {
// ADT-Tweak Start
//             var pierceDistance = ent.Comp.MaxDistance;
//             FireEffects(args.FromCoordinates, pierceDistance, args.ShotDirection.ToAngle(), ent.Owner);
// ADT-Tweak End
            var hitUid = hit.HitEntity;

// ADT-Tweak Start
//             foreach (var hit in rayCastResults)
//             {
//                 var hitUid = hit.HitEntity;
// ADT-Tweak End
            if (hit.Distance == 0f)
                continue;

// ADT-Tweak Start
//                 if (!_container.IsEntityOrParentInContainer(shooter) &&
//                     hitUid != args.Target &&
//                     CompOrNull<RequireProjectileTargetComponent>(hitUid)?.Active == true)
//                     continue;
// ADT-Tweak End
            if (!_container.IsEntityOrParentInContainer(shooter) &&
                hitUid != args.Target &&
                CompOrNull<RequireProjectileTargetComponent>(hitUid)?.Active == true)
                continue;

// ADT-Tweak Start
//                 var pierceData = new HitscanRaycastFiredData
//                 {
//                     ShotDirection = args.ShotDirection,
//                     Gun = args.Gun,
//                     Shooter = args.Shooter,
//                     HitEntity = hitUid,
//                 };
// ADT-Tweak End
            var pierceData = new HitscanRaycastFiredData
            {
                ShotDirection = args.ShotDirection,
                Gun = args.Gun,
                Shooter = args.Shooter,
                HitEntity = hitUid,
                HitPosition = hit.HitPos,
            };

// ADT-Tweak Start
//                 var pierceAttemptEvent = new AttemptHitscanRaycastFiredEvent { Data = pierceData };
//                 RaiseLocalEvent(ent, ref pierceAttemptEvent);
// ADT-Tweak End
            var pierceAttemptEvent = new AttemptHitscanRaycastFiredEvent { Data = pierceData };
            RaiseLocalEvent(ent, ref pierceAttemptEvent);

// ADT-Tweak Start
//                 if (pierceAttemptEvent.Cancelled)
//                     continue;
// ADT-Tweak End
            if (pierceAttemptEvent.Cancelled)
                continue;

// ADT-Tweak Start
//                 var pierceHitEvent = new HitscanRaycastFiredEvent { Data = pierceData };
//                 RaiseLocalEvent(ent, ref pierceHitEvent);
//             }
//             return;
// ADT-Tweak End
            var pierceHitEvent = new HitscanRaycastFiredEvent { Data = pierceData };
            RaiseLocalEvent(ent, ref pierceHitEvent);
        }
// ADT-Tweak Start
//         // ADT-Tweak BSA end
//
//         var target = args.Target;
//         // If you are in a container, use the raycast result
//         // Otherwise:
//         //  1.) Hit the first entity that you targeted.
//         //  2.) Hit the first entity that doesn't require you to aim at it specifically to be hit.
//         var result = _container.IsEntityOrParentInContainer(shooter)
//             ? rayCastResults.FirstOrNull()
//             : rayCastResults.FirstOrNull(hit => hit.HitEntity == target
//                                                 || CompOrNull<RequireProjectileTargetComponent>(hit.HitEntity)?.Active != true);
// ADT-Tweak End
    }

    private void FireSingleHit(Entity<HitscanBasicRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        var shooter = args.Shooter ?? args.Gun;
        var ignored = args.IgnoredEntity;
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);

        // ADT hitscan #3142: distance-based MobMover hit chance toward aim pointer
        var toMap = _transform.ToMapCoordinates(args.ToCoordinates);
        var pointer = (toMap.Position - mapCords.Position).Length();

        var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) ent.Comp.CollisionMask);
        // Ignore original shooter and the entity we just left (pierce/ricochet/reflect).
        var rayCastResults = _physics.IntersectRayWithPredicate(
            mapCords.MapId,
            ray,
            ent.Comp.MaxDistance,
            uid => uid == shooter || uid == ignored,
            false).ToList();

        var result = SelectHit(ent, shooter, mapCords, args.ShotDirection, pointer, args.Target, rayCastResults, args.PredictionSeed);

        var distanceTried = result?.Distance ?? ent.Comp.MaxDistance;

// ADT-Tweak Start
//         // Do visuals without an event. They should always happen and putting it on the attempt event is weird!
//         // If more stuff gets added here, it should probably be turned into an event.
//         FireEffects(args.FromCoordinates, distanceTried, args.ShotDirection.ToAngle(), ent.Owner);
// ADT-Tweak End
        var isRoot = false;
        if (args.OutputTrace is null)
        {
            args.OutputTrace = new List<HitscanTrace>();
            isRoot = true;
        }

        args.OutputTrace.Add(GenerateTraceStep(args.FromCoordinates, distanceTried, args.ShotDirection.ToAngle(), result?.HitEntity));

// ADT-Tweak Start
//         // Admin logging
// ADT-Tweak End
        if (result?.HitEntity != null)
        {
            _log.Add(LogType.HitScanHit,
                $"{ToPrettyString(shooter):user} hit {ToPrettyString(result.Value.HitEntity):target}"
                + $" using {ToPrettyString(args.Gun):entity}.");
        }

        var data = new HitscanRaycastFiredData
        {
            ShotDirection = args.ShotDirection,
            Gun = args.Gun,
            Hitscan = ent.Owner,
            Shooter = args.Shooter,
            HitEntity = result?.HitEntity,
            HitPosition = result?.HitPos,
            OutputTrace = args.OutputTrace,
            PredictionSeed = args.PredictionSeed,
        };

        var attemptEvent = new AttemptHitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
        {
            if (isRoot)
                FireTraceEffects(ent, args.OutputTrace, args.Shooter, args.Gun, args.PredictionSeed);
            return;
        }

        var hitEvent = new HitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref hitEvent);

        if (data.HitEntity != null)
        {
            var strikeEvent = new HitscanRaycastStrikeEvent { Data = data };
            RaiseLocalEvent(data.HitEntity.Value, ref strikeEvent);
        }

        if (isRoot)
            FireTraceEffects(ent, args.OutputTrace, args.Shooter, args.Gun, args.PredictionSeed);
    }

    private HitscanTrace GenerateTraceStep(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid? entity = null)
    {
        var fromXform = Transform(fromCoordinates.EntityId);

        var gridUid = fromXform.GridUid;
        if (gridUid != fromCoordinates.EntityId && TryComp(gridUid, out TransformComponent? gridXform))
        {
            var (_, gridRot, gridInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
            var map = _transform.ToMapCoordinates(fromCoordinates);
            fromCoordinates = new EntityCoordinates(gridUid.Value, Vector2.Transform(map.Position, gridInvMatrix));
            shotAngle -= gridRot;
        }
        else
        {
            shotAngle -= _transform.GetWorldRotation(fromXform);
        }

        var shotVec = shotAngle.ToVec().Normalized();

        return new HitscanTrace
        {
            Angle = shotAngle,
            Distance = distance,
            MuzzleCoordinates = distance > 1f ? GetNetCoordinates(fromCoordinates.Offset(shotVec / 2)) : null,
            // Midpoint of the shot. The beam sprite is centered here and grows from the muzzle to the impact.
            TravelCoordinates = distance > 1f ? GetNetCoordinates(fromCoordinates.Offset(shotVec * (distance / 2f))) : null,
            ImpactCoordinates = GetNetCoordinates(fromCoordinates.Offset(shotVec * distance)),
            ImpactedEnt = entity is { } uid ? GetNetEntity(uid) : null,
        };
    }

    private void FireTraceEffects(EntityUid hitscan, List<HitscanTrace> traces, EntityUid? shooter, EntityUid gun, int? predictionSeed)
    {
        if (!_visualsQuery.TryComp(hitscan, out var visuals))
            return;

        var hitscanEvent = new SharedGunSystem.HitscanEvent
        {
            MuzzleFlash = visuals.MuzzleFlash,
            TravelFlash = visuals.TravelFlash,
            ImpactFlash = visuals.ImpactFlash,
            Bullet = visuals.Bullet,
            Speed = visuals.Speed,
            Lifetime = visuals.EffectLifetime,
            MuzzleLinger = visuals.MuzzleLinger,
            TravelLinger = visuals.TravelLinger,
            ImpactLinger = visuals.ImpactLinger,
            BeamStartScale = visuals.BeamStartScale,
            FlashThickness = visuals.FlashThickness,
            ImpactSparks = visuals.ImpactSparks?.Id,
            BulletHole = visuals.BulletHole?.Id,
            BulletHoleOffset = visuals.BulletHoleOffset,
            Traces = traces,
            Prototype = MetaData(hitscan).EntityPrototype?.ID,
            Shooter = shooter is { } shotBy ? GetNetEntity(shotBy) : null,
            Gun = GetNetEntity(gun),
            PredictionSeed = predictionSeed,
        };

        // One PVS sample from the shot start + end is enough; merging PVS per segment was expensive on multi-bounce.
        var filter = Filter.Empty();
        EntityCoordinates? firstMuzzle = null;
        foreach (var trace in traces)
        {
            if (trace.MuzzleCoordinates is not { } netMuzzle)
                continue;

            var coords = GetCoordinates(netMuzzle);
            if (!coords.IsValid(EntityManager))
                continue;

            firstMuzzle = coords;
            break;
        }

        if (firstMuzzle is { } muzzlePos)
            filter = Filter.Pvs(muzzlePos, entityMan: EntityManager);

        if (traces.Count > 0)
            filter.Merge(Filter.Pvs(GetCoordinates(traces[^1].ImpactCoordinates), entityMan: EntityManager));

        RaiseNetworkEvent(hitscanEvent, filter);
    }

    /// <summary>
// ADT-Tweak Start
//     /// Create visual effects for the fired hitscan weapon.
// ADT-Tweak End
    /// BSA / legacy laser flash playback via sprite list.
    /// </summary>
// ADT-Tweak Start
//     /// <param name="fromCoordinates">Location to start the effect.</param>
//     /// <param name="distance">Distance of the hitscan shot.</param>
//     /// <param name="shotAngle">Angle of the shot.</param>
//     /// <param name="hitscanUid">The hitscan entity itself.</param>
//     private void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid hitscanUid)
// ADT-Tweak End
    private void FireLegacyEffects(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid hitscanUid)
    {
        if (distance == 0 || !_visualsQuery.TryComp(hitscanUid, out var vizComp))
            return;

        var sprites = new List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();
        var fromXform = Transform(fromCoordinates.EntityId);

// ADT-Tweak Start
//         // We'll get the effects relative to the grid / map of the firer
//         // Look you could probably optimise this a bit with redundant transforms at this point.
//
// ADT-Tweak End
        var gridUid = fromXform.GridUid;
        if (gridUid != fromCoordinates.EntityId && TryComp(gridUid, out TransformComponent? gridXform))
        {
            var (_, gridRot, gridInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
            var map = _transform.ToMapCoordinates(fromCoordinates);
            fromCoordinates = new EntityCoordinates(gridUid.Value, Vector2.Transform(map.Position, gridInvMatrix));
            shotAngle -= gridRot;
        }
        else
        {
            shotAngle -= _transform.GetWorldRotation(fromXform);
        }

        if (distance >= 1f)
        {
            if (vizComp.MuzzleFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec().Normalized() / 2);
// ADT-Tweak Start
//                 var netCoords = GetNetCoordinates(coords);
//
//                 sprites.Add((netCoords, shotAngle, vizComp.MuzzleFlash, 1f));
// ADT-Tweak End
                sprites.Add((GetNetCoordinates(coords), shotAngle, vizComp.MuzzleFlash, 1f));
            }

            if (vizComp.TravelFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec() * (distance + 0.5f) / 2);
// ADT-Tweak Start
//                 var netCoords = GetNetCoordinates(coords);
//
//                 sprites.Add((netCoords, shotAngle, vizComp.TravelFlash, distance - 1.5f));
// ADT-Tweak End
                sprites.Add((GetNetCoordinates(coords), shotAngle, vizComp.TravelFlash, distance - 1.5f));
            }
        }

        if (vizComp.ImpactFlash != null)
        {
            var coords = fromCoordinates.Offset(shotAngle.ToVec() * distance);
// ADT-Tweak Start
//             var netCoords = GetNetCoordinates(coords);
//
//             sprites.Add((netCoords, shotAngle.FlipPositive(), vizComp.ImpactFlash, 1f));
// ADT-Tweak End
            sprites.Add((GetNetCoordinates(coords), shotAngle.FlipPositive(), vizComp.ImpactFlash, 1f));
        }

        if (sprites.Count > 0)
        {
            RaiseNetworkEvent(new SharedGunSystem.HitscanEvent
            {
                Sprites = sprites,
// ADT-Tweak Start
//                 Lifetime = vizComp.EffectLifetime, // ADT-Tweak BSA 
// ADT-Tweak End
                Lifetime = vizComp.EffectLifetime,
            }, Filter.Pvs(fromCoordinates, entityMan: EntityManager));
        }
    }
}
