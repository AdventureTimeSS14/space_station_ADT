using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Projectiles;
using Content.Shared.ADT.EmitterMirror;
using Content.Shared.Whitelist;
using Content.Shared.Weapons.Ranged.Components;
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
        SubscribeLocalEvent<EmitterMirrorComponent,  ProjectileReflectAttemptEvent>(OnProjectileMirrorAttempt);
    }

    #region Vector Operations

    /// <summary>
    /// Gets the offset vector for a projectile reflection based on mirror settings.
    /// </summary>
    private Vector2? GetMirrorOffset(EmitterMirrorComponent component, Direction collisionDirection)
    {
        return component.TrinaryReflector
            ? component.TrinaryMirrorDirection?.ToVec()
            : component.BinaryReflector
                ? ReflectAngular(collisionDirection)
                : null;
    }

    /// <summary>
    /// Returns a directional vector based on the cardinal direction for binary reflection.
    /// </summary>
    private Vector2 ReflectAngular(Direction collisionDirection)
    {
        var directionToVector = new Dictionary<Direction, Vector2>
        {
            { Direction.North, Vector2.UnitY  },
            { Direction.South, -Vector2.UnitY },
            { Direction.East, -Vector2.UnitX  },
            { Direction.West, Vector2.UnitX   }
        };

        return directionToVector.TryGetValue(collisionDirection, out var vector) ? vector : Vector2.Zero;
    }

    #endregion

    #region Mirror Logic 

    /// <summary>
    /// Determines whether a projectile can be reflected based on direction, whitelist, and reflective properties.
    /// </summary>
    private bool CanReflectProjectile(EntityUid uid, EntityUid projectile, EmitterMirrorComponent component, out Direction? direction)
    {
        direction = GetImpactDirection(uid, projectile);

        if (direction == null)
            return false;

        if (!TryComp<ReflectiveComponent>(projectile, out var reflective))
            return false;

        if (reflective.Reflective == 0x0)
            return false;

        if (!TryComp<GunComponent>(uid, out var gunComponent))
            return false;

        if (_whitelistService.IsWhitelistFail(component.Whitelist, projectile))
            return false;

        if (component.BlockedDirections.Contains(direction.Value.ToString()))
            return false;

        return true;
    }

    /// <summary>
    /// Handles the event where a projectile attempts to reflect off a mirror.
    /// </summary>
    private void OnProjectileMirrorAttempt(EntityUid uid, EmitterMirrorComponent component, ref ProjectileReflectAttemptEvent args)
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