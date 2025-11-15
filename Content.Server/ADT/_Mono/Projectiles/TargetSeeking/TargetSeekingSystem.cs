using System.Numerics;
using Content.Shared.Interaction;
using Content.Server.Shuttles.Components;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;

namespace Content.Server.ADT._Mono.Projectiles.TargetSeeking;

/// <summary>
/// Handles the logic for target-seeking projectiles.
/// </summary>
public sealed class TargetSeekingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = null!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = null!;
    [Dependency] private readonly PhysicsSystem _physics = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TargetSeekingComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<TargetSeekingComponent, ComponentInit>(OnComponentInit);
    }

    /// <summary>
    /// Initialize the target seeking component with information about its origin grid.
    /// </summary>
    private void OnComponentInit(EntityUid uid, TargetSeekingComponent component, ComponentInit args)
    {
        if (TryComp<ProjectileComponent>(uid, out var projectile) &&
            projectile.Shooter.HasValue &&
            TryComp<TransformComponent>(projectile.Shooter.Value, out var shooterTransform))
        {
            component.OriginGridUid = shooterTransform.GridUid;
        }
    }

    /// <summary>
    /// Called when a target-seeking projectile hits something.
    /// </summary>
    private void OnProjectileHit(EntityUid uid, TargetSeekingComponent component, ref ProjectileHitEvent args)
    {
        // If we hit our actual target, we could perform additional effects here
        if (component.CurrentTarget.HasValue && component.CurrentTarget.Value == args.Target)
        {
            // Target hit successfully
        }

        // Reset the target since we've hit something
        component.CurrentTarget = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TargetSeekingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var seekingComp, out var xform))
        {
            // Initialize speed if needed
            if (seekingComp.CurrentSpeed < seekingComp.LaunchSpeed)
            {
                seekingComp.CurrentSpeed = seekingComp.LaunchSpeed;
            }

            // Accelerate up to max speed
            if (seekingComp.CurrentSpeed < seekingComp.MaxSpeed)
            {
                seekingComp.CurrentSpeed += seekingComp.Acceleration * frameTime;
            }
            else
            {
                seekingComp.CurrentSpeed = seekingComp.MaxSpeed;
            }

            // Apply velocity in the direction the projectile is facing
            _physics.SetLinearVelocity(uid, _transform.GetWorldRotation(xform).ToWorldVec() * seekingComp.CurrentSpeed);

            // If we have a target, track it using the selected algorithm
            if (seekingComp.CurrentTarget.HasValue)
            {
                if (seekingComp.TrackingAlgorithm == TrackingMethod.Predictive)
                {
                    ApplyPredictiveTracking(uid, seekingComp, xform, frameTime);
                }
                else
                {
                    ApplyDirectTracking(uid, seekingComp, xform, frameTime);
                }
            }
            else
            {
                // Try to acquire a new target
                AcquireTarget(uid, seekingComp, xform);
            }
        }
    }

    /// <summary>
    /// Finds the closest valid target within range and tracking parameters.
    /// </summary>
    public void AcquireTarget(EntityUid uid, TargetSeekingComponent component, TransformComponent transform)
    {
        var closestDistance = float.MaxValue;
        EntityUid? bestTarget = null;
        TransformComponent? bestTargetXform = null;

        // Look for shuttles to target
        var shuttleQuery = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();

        while (shuttleQuery.MoveNext(out var targetUid, out _, out var targetXform))
        {
            EntityUid effectiveTargetToConsider;
            TransformComponent currentCandidateXform;

            if (targetXform.GridUid.HasValue)
            {
                effectiveTargetToConsider = targetXform.GridUid.Value;
                if (!TryComp<TransformComponent>(effectiveTargetToConsider, out var gridXform))
                {
                    continue;
                }
                currentCandidateXform = gridXform;
            }
            else
            {
                effectiveTargetToConsider = targetUid;
                currentCandidateXform = targetXform;
            }

            if (component.OriginGridUid.HasValue &&
                effectiveTargetToConsider == component.OriginGridUid.Value)
            {
                continue;
            }

            var targetPos = _transform.ToMapCoordinates(currentCandidateXform.Coordinates).Position;
            var sourcePos = _transform.ToMapCoordinates(transform.Coordinates).Position;
            var angleToTarget = (targetPos - sourcePos).ToWorldAngle();

            var currentRotation = _transform.GetWorldRotation(transform);

            var angleDifference = Angle.ShortestDistance(currentRotation, angleToTarget).Degrees;
            if (MathF.Abs((float)angleDifference) > component.FieldOfView / 2)
            {
                continue;
            }

            var distance = Vector2.Distance(sourcePos, targetPos);

            if (distance > component.DetectionRange)
            {
                continue;
            }

            if (closestDistance > distance)
            {
                closestDistance = distance;
                bestTarget = effectiveTargetToConsider;
                bestTargetXform = currentCandidateXform;
            }
        }

        if (bestTarget.HasValue && bestTargetXform != null)
        {
            component.CurrentTarget = bestTarget;
            component.PreviousTargetPosition = _transform.ToMapCoordinates(bestTargetXform.Coordinates).Position;
            component.PreviousDistance = closestDistance;
        }
        else if (bestTarget.HasValue && bestTargetXform == null)
        {
            if (TryComp<TransformComponent>(bestTarget.Value, out var recoveredXform))
            {
                component.CurrentTarget = bestTarget;
                component.PreviousTargetPosition = _transform.ToMapCoordinates(recoveredXform.Coordinates).Position;
                component.PreviousDistance = closestDistance;
            }
            else
            {
                component.CurrentTarget = null;
            }
        }
        else
        {
            component.CurrentTarget = null;
        }
    }

    /// <summary>
    /// Advanced tracking that predicts where the target will be based on its velocity.
    /// </summary>
    public void ApplyPredictiveTracking(EntityUid uid, TargetSeekingComponent comp, TransformComponent xform, float frameTime)
    {
        if (!comp.CurrentTarget.HasValue || !TryComp<TransformComponent>(comp.CurrentTarget.Value, out var targetXform))
        {
            return;
        }

        // Get current positions
        var currentTargetPosition = _transform.ToMapCoordinates(targetXform.Coordinates).Position;
        var sourcePosition = _transform.ToMapCoordinates(xform.Coordinates).Position;

        // Calculate current distance
        var currentDistance = Vector2.Distance(sourcePosition, currentTargetPosition);

        // Calculate target velocity
        var targetVelocity = currentTargetPosition - comp.PreviousTargetPosition;

        // Calculate time to intercept (using closing rate)
        var closingRate = (comp.PreviousDistance - currentDistance);
        var timeToIntercept = closingRate > 0.01f ?
            currentDistance / closingRate :
            currentDistance / comp.CurrentSpeed;

        // Prevent negative or very small intercept times that could cause erratic behavior
        timeToIntercept = MathF.Max(timeToIntercept, 0.1f);

        // Predict where the target will be when we reach it
        var predictedPosition = currentTargetPosition + (targetVelocity * timeToIntercept);

        // Calculate angle to the predicted position
        var targetAngle = (predictedPosition - sourcePosition).ToWorldAngle();

        // Rotate toward that angle at our turn rate
        _rotateToFace.TryRotateTo(
            uid,
            targetAngle,
            frameTime,
            comp.ScanArc,
            comp.TurnRate?.Theta ?? MathF.PI * 2,
            xform
        );

        // Update tracking data for next frame
        comp.PreviousTargetPosition = currentTargetPosition;
        comp.PreviousDistance = currentDistance;
    }

    /// <summary>
    /// Basic tracking that points directly at the current target position.
    /// </summary>
    public void ApplyDirectTracking(EntityUid uid, TargetSeekingComponent comp, TransformComponent xform, float frameTime)
    {
        if (!comp.CurrentTarget.HasValue || !TryComp<TransformComponent>(comp.CurrentTarget.Value, out var targetXform))
        {
            return;
        }

        // Get the angle directly toward the target
        var angleToTarget = (
            _transform.ToMapCoordinates(targetXform.Coordinates).Position -
            _transform.ToMapCoordinates(xform.Coordinates).Position
        ).ToWorldAngle();

        // Rotate toward that angle at our turn rate
        _rotateToFace.TryRotateTo(
            uid,
            angleToTarget,
            frameTime,
            comp.ScanArc,
            comp.TurnRate?.Theta ?? MathF.PI * 2,
            xform
        );
    }
}
