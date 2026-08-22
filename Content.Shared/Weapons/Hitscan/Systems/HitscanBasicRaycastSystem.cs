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

public sealed class HitscanBasicRaycastSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _rand = default!; // ADT hitscan #3142

    private EntityQuery<HitscanBasicVisualsComponent> _visualsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _visualsQuery = GetEntityQuery<HitscanBasicVisualsComponent>();

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
        var rayCastResults = _physics.IntersectRayWithPredicate(
            mapCords.MapId,
            ray,
            ent.Comp.MaxDistance,
            uid => uid == shooter || uid == ignored,
            false);

        FireLegacyEffects(args.FromCoordinates, ent.Comp.MaxDistance, args.ShotDirection.ToAngle(), ent.Owner);

        foreach (var hit in rayCastResults)
        {
            var hitUid = hit.HitEntity;

            if (hit.Distance == 0f)
                continue;

            if (!_container.IsEntityOrParentInContainer(shooter) &&
                hitUid != args.Target &&
                CompOrNull<RequireProjectileTargetComponent>(hitUid)?.Active == true)
                continue;

            var pierceData = new HitscanRaycastFiredData
            {
                ShotDirection = args.ShotDirection,
                Gun = args.Gun,
                Shooter = args.Shooter,
                HitEntity = hitUid,
                HitPosition = hit.HitPos,
            };

            var pierceAttemptEvent = new AttemptHitscanRaycastFiredEvent { Data = pierceData };
            RaiseLocalEvent(ent, ref pierceAttemptEvent);

            if (pierceAttemptEvent.Cancelled)
                continue;

            var pierceHitEvent = new HitscanRaycastFiredEvent { Data = pierceData };
            RaiseLocalEvent(ent, ref pierceHitEvent);
        }
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

        RayCastResults? result = null;

        if (_container.IsEntityOrParentInContainer(shooter))
        {
            result = rayCastResults.Count == 0 ? null : rayCastResults[0];
        }
        else
        {
            foreach (var collide in rayCastResults)
            {
                // Starlight: zero-distance hits cause self-trapped rays / self-crits.
                if (collide.Distance == 0f)
                    continue;

                if (collide.HitEntity != args.Target &&
                    CompOrNull<RequireProjectileTargetComponent>(collide.HitEntity)?.Active == true)
                    continue;

                if (collide.Distance < pointer - 2f && HasComp<MobMoverComponent>(collide.HitEntity))
                {
                    if (pointer - collide.Distance > 4f)
                        continue;

                    var chance = Math.Clamp(1f - ((collide.Distance - 2f) / 2f), 0f, 1f);
                    if (!_rand.Prob(chance))
                        continue;
                }

                result = collide;
                break;
            }
        }

        var distanceTried = result?.Distance ?? ent.Comp.MaxDistance;

        var isRoot = false;
        if (args.OutputTrace is null)
        {
            args.OutputTrace = new List<HitscanTrace>();
            isRoot = true;
        }

        args.OutputTrace.Add(GenerateTraceStep(args.FromCoordinates, distanceTried, args.ShotDirection.ToAngle(), result?.HitEntity));

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
            Shooter = args.Shooter,
            HitEntity = result?.HitEntity,
            HitPosition = result?.HitPos,
            OutputTrace = args.OutputTrace,
        };

        var attemptEvent = new AttemptHitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
        {
            if (isRoot)
                FireTraceEffects(ent, args.OutputTrace);
            return;
        }

        var hitEvent = new HitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref hitEvent);

        if (isRoot)
            FireTraceEffects(ent, args.OutputTrace);
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
            TravelCoordinates = distance > 1f ? GetNetCoordinates(fromCoordinates.Offset(shotVec * (distance + 0.5f) / 2)) : null,
            ImpactCoordinates = GetNetCoordinates(fromCoordinates.Offset(shotVec * distance)),
            ImpactedEnt = entity is { } uid ? GetNetEntity(uid) : null,
        };
    }

    private void FireTraceEffects(EntityUid hitscan, List<HitscanTrace> traces)
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
            Traces = traces,
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
    /// BSA / legacy laser flash playback via sprite list.
    /// </summary>
    private void FireLegacyEffects(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid hitscanUid)
    {
        if (distance == 0 || !_visualsQuery.TryComp(hitscanUid, out var vizComp))
            return;

        var sprites = new List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();
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

        if (distance >= 1f)
        {
            if (vizComp.MuzzleFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec().Normalized() / 2);
                sprites.Add((GetNetCoordinates(coords), shotAngle, vizComp.MuzzleFlash, 1f));
            }

            if (vizComp.TravelFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec() * (distance + 0.5f) / 2);
                sprites.Add((GetNetCoordinates(coords), shotAngle, vizComp.TravelFlash, distance - 1.5f));
            }
        }

        if (vizComp.ImpactFlash != null)
        {
            var coords = fromCoordinates.Offset(shotAngle.ToVec() * distance);
            sprites.Add((GetNetCoordinates(coords), shotAngle.FlipPositive(), vizComp.ImpactFlash, 1f));
        }

        if (sprites.Count > 0)
        {
            RaiseNetworkEvent(new SharedGunSystem.HitscanEvent
            {
                Sprites = sprites,
                Lifetime = vizComp.EffectLifetime,
            }, Filter.Pvs(fromCoordinates, entityMan: EntityManager));
        }
    }
}
