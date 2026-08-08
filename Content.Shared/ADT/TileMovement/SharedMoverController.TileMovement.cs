using System.Numerics;
using Content.Shared.ADT.NPC;
using Content.Shared.ADT.TileMovement;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Tile based movement, ported from vg station && goob station
/// <summary>
public abstract partial class SharedMoverController
{
    protected EntityQuery<TileMovementComponent> TileMovementQuery;
    protected EntityQuery<NPCTileMovementComponent> NpcTileMovementQuery;
    protected EntityQuery<ActorComponent> ActorQuery;

    private TimeSpan TileMovementCurrentTime => PhysicsSystem.EffectiveCurTime ?? Timing.CurTime;

    /// <summary>
    /// How close to the destination an NPC has to get before its slide is considered finished
    /// </summary>
    private const float NpcDestinationTolerance = 0.05f;

    public void InitializeTileMovement()
    {
        TileMovementQuery = GetEntityQuery<TileMovementComponent>();
        NpcTileMovementQuery = GetEntityQuery<NPCTileMovementComponent>();
        ActorQuery = GetEntityQuery<ActorComponent>();
    }

    /// <summary>
    ///     Runs one tick of tile-based movement on the given inputs.
    /// </summary>
    /// <param name="uid">UID of the entity doing the move.</param>
    /// <param name="tileMovement">TileMovementComponent on the entity doing the move.</param>
    /// <param name="physicsComponent">PhysicsComponent on the entity doing the move.</param>
    /// <param name="targetTransform">TransformComponent on the entity doing the move.</param>
    /// <param name="inputMover">InputMoverComponent on the entity doing the move.</param>
    /// <param name="tileDef">ContentTileDefinition of the tile underneath the entity doing the move, if there is one.</param>
    /// <param name="relaySource">Source of the movement relay, if any.</param>
    /// <param name="frameTime">Time in seconds since the last tick of the physics system.</param>
    /// <returns>True if tile movement handled this tick and normal movement should be skipped.</returns>
    public bool HandleTileMovement(
        EntityUid uid,
        TileMovementComponent tileMovement,
        PhysicsComponent physicsComponent,
        TransformComponent targetTransform,
        InputMoverComponent inputMover,
        ContentTileDefinition? tileDef,
        EntityUid? relaySource,
        float frameTime)
    {
        var isNpc = NpcTileMovementQuery.HasComponent(uid) && !ActorQuery.HasComponent(uid);
        var wishButtons = GetTileMoveButtons(uid, inputMover, isNpc);

        if (tileMovement.WasWeightlessLastTick)
        {
            InitializeSlideToCenter(uid, tileMovement);
            UpdateSlide(uid, tileMovement, inputMover);
        }
        else if (wishButtons == MoveButtons.None && !tileMovement.SlideActive)
        {
            var movementVelocity = physicsComponent.LinearVelocity;

            var movementSpeedComponent = ModifierQuery.CompOrNull(uid);
            var friction = GetTileMovementFriction(inputMover, movementSpeedComponent, tileDef);
            var minimumFrictionSpeed = movementSpeedComponent?.MinimumFrictionSpeed ??
                MovementSpeedModifierComponent.DefaultMinimumFrictionSpeed;
            Friction(minimumFrictionSpeed, frameTime, friction, ref movementVelocity);

            PhysicsSystem.SetLinearVelocity(uid, movementVelocity, body: physicsComponent);
            PhysicsSystem.SetAngularVelocity(uid, 0, body: physicsComponent);
        }
        else
        {
            PlayTileMovementFootstep(uid, inputMover, targetTransform, tileDef, relaySource);

            if (tileMovement.SlideActive)
            {
                var movementSpeed = GetEntityMoveSpeed(uid, inputMover.Sprinting);

                if (CheckForSlideEnd(wishButtons, targetTransform, tileMovement, movementSpeed, isNpc))
                {
                    EndSlide(uid, tileMovement);

                    if (wishButtons != MoveButtons.None)
                    {
                        InitializeSlide(uid, tileMovement, inputMover, wishButtons);
                        UpdateSlide(uid, tileMovement, inputMover);
                        tileMovement.FailureSlideActive = false;
                    }
                    else if (!tileMovement.FailureSlideActive
                             && !targetTransform.LocalPosition.EqualsApprox(tileMovement.Destination, 0.04))
                    {
                        InitializeSlideToTarget(uid, tileMovement, targetTransform.LocalPosition, MoveButtons.None);
                        UpdateSlide(uid, tileMovement, inputMover);
                        tileMovement.FailureSlideActive = true;
                    }
                    else
                    {
                        ForceSnapToTile(uid, inputMover);
                        tileMovement.FailureSlideActive = false;
                    }
                }
                else if (tileMovement.Origin.EntityId != targetTransform.ParentUid)
                {
                    var previousButtons = tileMovement.CurrentSlideMoveButtons;
                    var previousInitialKeyDownTime = tileMovement.MovementKeyInitialDownTime;
                    InitializeSlideToCenter(uid, tileMovement);
                    tileMovement.CurrentSlideMoveButtons = previousButtons;
                    tileMovement.MovementKeyInitialDownTime = previousInitialKeyDownTime;
                    UpdateSlide(uid, tileMovement, inputMover);
                }
                else
                {
                    UpdateSlide(uid, tileMovement, inputMover);
                }
            }
            else
            {
                InitializeSlide(uid, tileMovement, inputMover, wishButtons);
                UpdateSlide(uid, tileMovement, inputMover);
            }

            if (!NoRotateQuery.HasComponent(uid) && !tileMovement.FailureSlideActive)
            {
                if (tileMovement.SlideActive
                    && TryComp(inputMover.RelativeEntity, out TransformComponent? parentTransform))
                {
                    var delta = tileMovement.Destination - tileMovement.Origin.Position;
                    var worldRot = _transform.GetWorldRotation(parentTransform).RotateVec(delta).ToWorldAngle();
                    _transform.SetWorldRotation(targetTransform, worldRot);
                }
            }
        }

        tileMovement.LastTickLocalCoordinates = targetTransform.LocalPosition;
        Dirty(uid, tileMovement);
        return true;
    }

    private MoveButtons GetTileMoveButtons(EntityUid uid, InputMoverComponent inputMover, bool isNpc)
    {
        if (!isNpc)
            return StripWalk(inputMover.HeldMoveButtons);

        var wish = inputMover.CurTickSprintMovement;

        if (wish.LengthSquared() < 0.01f)
            return MoveButtons.None;

        wish = (-inputMover.TargetRelativeRotation).RotateVec(wish);

        var buttons = MoveButtons.None;

        var threshold = Math.Max(Math.Abs(wish.X), Math.Abs(wish.Y)) * 0.5f;

        if (wish.X > threshold)
            buttons |= MoveButtons.Right;
        else if (wish.X < -threshold)
            buttons |= MoveButtons.Left;

        if (wish.Y > threshold)
            buttons |= MoveButtons.Up;
        else if (wish.Y < -threshold)
            buttons |= MoveButtons.Down;

        return buttons;
    }

    private void PlayTileMovementFootstep(
        EntityUid uid,
        InputMoverComponent inputMover,
        TransformComponent targetTransform,
        ContentTileDefinition? tileDef,
        EntityUid? relaySource)
    {
        if (!MobMoverQuery.TryGetComponent(uid, out var mobMover))
            return;

        if (!TryGetSound(false, uid, inputMover, mobMover, targetTransform, out var sound, tileDef: tileDef))
            return;

        var soundModifier = inputMover.Sprinting
            ? InputMoverComponent.SprintingSoundModifier
            : InputMoverComponent.WalkingSoundModifier;

        var audioParams = sound.Params
            .WithVolume(sound.Params.Volume + soundModifier)
            .WithVariation(sound.Params.Variation ?? mobMover.FootstepVariation);

        if (relaySource != null)
        {
            _audio.PlayPredicted(sound, uid, relaySource.Value, audioParams);
        }
        else
        {
            _audio.PlayPredicted(sound, uid, uid, audioParams);
        }
    }

    private bool CheckForSlideEnd(
        MoveButtons pressedButtons,
        TransformComponent transform,
        TileMovementComponent tileMovement,
        float movementSpeed,
        bool isNpc)
    {
        var distanceToDestination = (tileMovement.Destination - tileMovement.Origin.Position).Length();
        var minPressedTime = Math.Min(1.05f / movementSpeed * distanceToDestination, 20);

        var destinationTolerance = isNpc
            ? NpcDestinationTolerance
            : movementSpeed / 100f;

        var reachedDestination = transform.LocalPosition.EqualsApprox(tileMovement.Destination, destinationTolerance);
        var hardDurationLimitPassed =
            TileMovementCurrentTime - tileMovement.MovementKeyInitialDownTime >= TimeSpan.FromSeconds(minPressedTime) * 3;

        if (isNpc)
            return reachedDestination || hardDurationLimitPassed;

        var stoppedPressing = pressedButtons != tileMovement.CurrentSlideMoveButtons;
        var minDurationPassed =
            TileMovementCurrentTime - tileMovement.MovementKeyInitialDownTime >= TimeSpan.FromSeconds(minPressedTime);
        var noProgress = tileMovement.LastTickLocalCoordinates != null
                         && transform.LocalPosition.EqualsApprox(tileMovement.LastTickLocalCoordinates.Value, destinationTolerance / 3);

        return reachedDestination || (stoppedPressing && (minDurationPassed || noProgress)) || hardDurationLimitPassed;
    }

    private void InitializeSlideToTarget(
        EntityUid uid,
        TileMovementComponent tileMovement,
        Vector2 localPositionTarget,
        MoveButtons heldMoveButtons)
    {
        var transform = Transform(uid);
        var localPosition = transform.LocalPosition;

        tileMovement.SlideActive = true;
        tileMovement.Origin = new EntityCoordinates(transform.ParentUid, localPosition);
        tileMovement.Destination = SnapCoordinatesToTile(localPositionTarget);
        tileMovement.MovementKeyInitialDownTime = TileMovementCurrentTime;
        tileMovement.CurrentSlideMoveButtons = heldMoveButtons;
    }

    private void InitializeSlideToCenter(EntityUid uid, TileMovementComponent tileMovement)
    {
        var localPosition = Transform(uid).LocalPosition;
        InitializeSlideToTarget(uid, tileMovement, SnapCoordinatesToTile(localPosition), MoveButtons.None);
    }

    private void InitializeSlide(
        EntityUid uid,
        TileMovementComponent tileMovement,
        InputMoverComponent inputMover,
        MoveButtons wishButtons)
    {
        var transform = Transform(uid);
        var localPosition = transform.LocalPosition;
        var offset = DirVecForButtons(wishButtons);
        offset = inputMover.TargetRelativeRotation.RotateVec(offset);

        var origin = SnapCoordinatesToTile(localPosition);

        InitializeSlideToTarget(uid, tileMovement, origin + offset, wishButtons);
    }

    private void UpdateSlide(EntityUid uid, TileMovementComponent tileMovement, InputMoverComponent inputMover)
    {
        var targetTransform = Transform(uid);

        if (!PhysicsQuery.TryComp(uid, out var physicsComponent))
            return;

        var moveSpeedComponent = ModifierQuery.CompOrNull(uid);
        var parentRotation = Angle.Zero;

        if (XformQuery.TryGetComponent(targetTransform.GridUid, out var relativeTransform))
            parentRotation = _transform.GetWorldRotation(relativeTransform);

        var movementVelocity = tileMovement.Destination - targetTransform.LocalPosition;
        movementVelocity.Normalize();

        if (inputMover.Sprinting)
        {
            movementVelocity *= moveSpeedComponent?.CurrentSprintSpeed ??
                MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        }
        else
        {
            movementVelocity *= moveSpeedComponent?.CurrentWalkSpeed ??
                MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        }

        movementVelocity = parentRotation.RotateVec(movementVelocity);

        PhysicsSystem.SetLinearVelocity(uid, movementVelocity, body: physicsComponent);
        PhysicsSystem.SetAngularVelocity(uid, 0, body: physicsComponent);
    }

    private void EndSlide(EntityUid uid, TileMovementComponent tileMovement)
    {
        tileMovement.SlideActive = false;
        tileMovement.MovementKeyInitialDownTime = null;

        if (!PhysicsQuery.TryComp(uid, out var physicsComponent))
            return;

        PhysicsSystem.SetLinearVelocity(uid, Vector2.Zero, body: physicsComponent);
        PhysicsSystem.SetAngularVelocity(uid, 0, body: physicsComponent);
    }

    private void ForceSnapToTile(EntityUid uid, InputMoverComponent inputMover)
    {
        if (!TryComp(inputMover.RelativeEntity, out TransformComponent? _))
            return;

        var targetTransform = Transform(uid);
        var localCoordinates = targetTransform.LocalPosition;
        var snappedCoordinates = SnapCoordinatesToTile(localCoordinates);

        if (!localCoordinates.EqualsApprox(snappedCoordinates) && targetTransform.ParentUid.IsValid())
            _transform.SetLocalPosition(uid, snappedCoordinates);

        PhysicsSystem.WakeBody(uid);
    }

    private float GetEntityMoveSpeed(EntityUid uid, bool sprinting)
    {
        var moveSpeedComponent = ModifierQuery.CompOrNull(uid);

        if (sprinting)
            return moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

        return moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
    }

    private float GetTileMovementFriction(
        InputMoverComponent inputMover,
        MovementSpeedModifierComponent? movementSpeedComponent,
        ContentTileDefinition? tileDef)
    {
        if (inputMover.HeldMoveButtons != MoveButtons.None || movementSpeedComponent?.FrictionNoInput == null)
        {
            return tileDef?.MobFriction ??
                movementSpeedComponent?.Friction ?? MovementSpeedModifierComponent.DefaultFriction;
        }

        return movementSpeedComponent.FrictionNoInput;
    }

    private MoveButtons StripWalk(MoveButtons input)
    {
        return input & ~MoveButtons.Walk;
    }

    public static Vector2 SnapCoordinatesToTile(Vector2 input)
    {
        return new Vector2((int) MathF.Floor(input.X) + 0.5f, (int) MathF.Floor(input.Y) + 0.5f);
    }
}
