using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Movement.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Gravity;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Movement.Systems;

public abstract class SharedWaddleAnimationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaddleAnimationComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<WaddleAnimationComponent, MoveInputEvent>(OnMovementInput);
        SubscribeLocalEvent<WaddleAnimationComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref StunnedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref DownedEvent _) => StopWaddling(ent));
        SubscribeLocalEvent((Entity<WaddleAnimationComponent> ent, ref BuckledEvent _) => StopWaddling(ent));
        SubscribeLocalEvent<WaddleAnimationComponent, GravityChangedEvent>(OnGravityChanged);
    }

    private bool CanWaddle(EntityUid uid)
    {
        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return false;

        if (!_actionBlocker.CanMove(uid, mover))
            return false;

        if (_buckle.IsBuckled(uid))
            return false;

        if (_standing.IsDown(uid))
            return false;

        if (_mobState.IsIncapacitated(uid))
            return false;

        return true;
    }

    private void OnGravityChanged(Entity<WaddleAnimationComponent> ent, ref GravityChangedEvent args)
    {
        if (!args.HasGravity && ent.Comp.IsCurrentlyWaddling)
            StopWaddling(ent);
    }

    private void OnComponentStartup(Entity<WaddleAnimationComponent> entity, ref ComponentStartup args)
    {
        if (!TryComp<InputMoverComponent>(entity.Owner, out var moverComponent))
            return;

        if ((moverComponent.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        if (entity.Comp.IsCurrentlyWaddling)
            return;

        if (!CanWaddle(entity.Owner))
            return;

        entity.Comp.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(entity.Owner)));
    }

    private void OnMovementInput(Entity<WaddleAnimationComponent> entity, ref MoveInputEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!args.HasDirectionalMovement && entity.Comp.IsCurrentlyWaddling)
        {
            StopWaddling(entity);

            return;
        }

        if (entity.Comp.IsCurrentlyWaddling || !args.HasDirectionalMovement)
            return;

        if (!CanWaddle(entity.Owner))
            return;

        entity.Comp.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(entity.Owner)));
    }

    private void OnStood(Entity<WaddleAnimationComponent> entity, ref StoodEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        if (!TryComp<InputMoverComponent>(entity.Owner, out var mover))
        {
            return;
        }

        if ((mover.HeldMoveButtons & MoveButtons.AnyDirection) == MoveButtons.None)
            return;

        if (entity.Comp.IsCurrentlyWaddling)
            return;

        if (!CanWaddle(entity.Owner))
            return;

        if (_standing.IsDown(entity.Owner))
            return;

        entity.Comp.IsCurrentlyWaddling = true;

        RaiseNetworkEvent(new StartedWaddlingEvent(GetNetEntity(entity.Owner)));
    }

    private void StopWaddling(Entity<WaddleAnimationComponent> entity)
    {
        if (!entity.Comp.IsCurrentlyWaddling)
            return;

        entity.Comp.IsCurrentlyWaddling = false;

        RaiseNetworkEvent(new StoppedWaddlingEvent(GetNetEntity(entity.Owner)));
    }
}
