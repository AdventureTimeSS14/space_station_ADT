using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Projectiles;
using Content.Shared.ADT.EmitterMirror;
using Content.Shared.Whitelist;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Maths;

namespace Content.Server.ADT.EmitterMirror;

/// <summary>
/// System responsible for reflecting projectiles, with filtering via the Emitter Mirror Component whitelist.
/// </summary>
public sealed class EmitterMirrorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistService = default!;
    [Dependency] private readonly SharedTransformSystem _transformService = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmitterMirrorComponent,  ProjectileReflectAttemptEvent>(OnReflectMirrorAttempt);
    }

    #region Vector Operations

    /// <summary>
    /// Gets the offset vector for a projectile reflection based on mirror settings.
    /// </summary>
    private Vector2? GetMirrorOffset(EmitterMirrorComponent component, Direction collisionDirection)
    {
        if (component.TrinaryReflector)
            return component.TrinaryMirrorDirection?.ToVec();

        if (component.BinaryReflector &&
            EmitterMirrorComponent.DirectionToVector.TryGetValue(collisionDirection, out var result))
            return result;

        return null;
    }
    #endregion

    #region Mirror Logic 

    /// <summary>
    /// Determines whether a projectile can be reflected based on direction, whitelist, and reflective properties.
    /// </summary>
    private bool CanReflectProjectile(EntityUid uid, EntityUid projectile, EmitterMirrorComponent component, out Direction? direction)
    {
        direction = GetImpactDirection(uid, projectile);

        return direction != null
            && TryComp<ReflectiveComponent>(projectile, out var reflective)
            && reflective.Reflective != 0x0
            && TryComp<GunComponent>(uid, out _)
            && !_whitelistService.IsWhitelistFail(component.Whitelist, projectile)
            && !component.BlockedDirections.Contains(direction.Value.ToString());
    }

    /// <summary>
    /// Handles the event where a projectile attempts to reflect off a mirror.
    /// </summary>
    private void OnReflectMirrorAttempt(EntityUid uid, EmitterMirrorComponent component, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanReflectProjectile(uid, args.ProjUid, component, out var impactDirection))
            return;

        args.Cancelled = true;

        ProcessMirror(uid, args.ProjUid, component, impactDirection);
    }

    /// <summary>
    /// Calculates the direction from which a projectile hit the mirror, in the mirror's local space.
    /// </summary>
    private Direction? GetImpactDirection(EntityUid uid, EntityUid projectile)
    {
        var projectileWorldPos = _transformService.GetWorldPosition(projectile);
        var emittermirrorMatrixInv = _transformService.GetInvWorldMatrix(Transform(uid));

        var localImpactPosition = Vector2.Transform(projectileWorldPos, emittermirrorMatrixInv);

        return localImpactPosition.ToAngle().GetCardinalDir();
    }
    #endregion
     
    #region Spawn Projectile
    /// <summary>
    /// Reflects the projectile by adjusting its position and shooting it in a new direction.
    /// </summary>
    private void ProcessMirror(EntityUid uid, EntityUid projectile, EmitterMirrorComponent component, Direction? direction)
    {
        if (direction == null)
            return;

        var MirrorOffset = GetMirrorOffset(component, direction.Value);
        if (MirrorOffset == null)
            return;

        var emittermirrorTransform = Transform(uid);
        var adjustedOffset = emittermirrorTransform.LocalRotation.RotateVec(MirrorOffset.Value);
        var newProjectilePosition = emittermirrorTransform.LocalPosition + adjustedOffset;

        _transformService.SetLocalPosition(projectile, newProjectilePosition);

        var shootingCoords = new EntityCoordinates(uid, MirrorOffset.Value);
        _gunSystem.Shoot(uid, Comp<GunComponent>(uid), projectile, emittermirrorTransform.Coordinates, shootingCoords, out _);
    }
    #endregion
}