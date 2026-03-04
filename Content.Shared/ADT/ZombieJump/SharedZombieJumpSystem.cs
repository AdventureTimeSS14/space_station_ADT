using Content.Shared.ADT.ZombieJump;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.ADT.ZombieJump;
public abstract partial class SharedZombieJumpSystem : EntitySystem
{
    [Dependency] protected readonly ThrowingSystem Throwing = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedGravitySystem Gravity = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly StandingStateSystem Standing = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieJumpComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ZombieJumpComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ZombieJumpComponent, ZombieJumpEvent>(OnZombieJump);

        SubscribeLocalEvent<ActiveZombieLeaperComponent, StartCollideEvent>(OnLeaperCollide);
        SubscribeLocalEvent<ActiveZombieLeaperComponent, LandEvent>(OnLeaperLand);
        SubscribeLocalEvent<ActiveZombieLeaperComponent, StopThrowEvent>(OnLeaperStopThrow);
    }

    private void OnInit(Entity<ZombieJumpComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp(entity, out ActionsComponent? comp))
            return;

        Actions.AddAction(entity, ref entity.Comp.ActionEntity, entity.Comp.Action, component: comp);
    }

    private void OnShutdown(Entity<ZombieJumpComponent> entity, ref ComponentShutdown args)
    {
        Actions.RemoveAction(entity.Owner, entity.Comp.ActionEntity);
    }

    private void OnLeaperCollide(Entity<ActiveZombieLeaperComponent> entity, ref StartCollideEvent args)
    {
        if (args.OtherEntity != entity.Owner && !HasComp<ZombieComponent>(args.OtherEntity))
        {
            TryStunAndKnockdown(args.OtherEntity, entity.Comp.KnockdownDuration);
        }

        RemCompDeferred<ActiveZombieLeaperComponent>(entity);
    }

    private void OnLeaperLand(Entity<ActiveZombieLeaperComponent> entity, ref LandEvent args)
    {
        RemCompDeferred<ActiveZombieLeaperComponent>(entity);
    }

    private void OnLeaperStopThrow(Entity<ActiveZombieLeaperComponent> entity, ref StopThrowEvent args)
    {
        RemCompDeferred<ActiveZombieLeaperComponent>(entity);
    }

    protected virtual void OnZombieJump(Entity<ZombieJumpComponent> entity, ref ZombieJumpEvent args)
    {
        if (Gravity.IsWeightless(args.Performer))
        {
            if (entity.Comp.JumpFailedPopup != null)
                Popup.PopupClient(Loc.GetString(entity.Comp.JumpFailedPopup.Value), args.Performer, args.Performer);
            return;
        }

        if (!TryComp<TransformComponent>(args.Performer, out var xform))
            return;

        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.JumpDistance;
        var direction = xform.Coordinates.Offset(throwing);

        Throwing.TryThrow(args.Performer, direction, entity.Comp.JumpThrowSpeed);

        Audio.PlayPredicted(entity.Comp.JumpSound, args.Performer, args.Performer);

        EnsureComp<ActiveZombieLeaperComponent>(entity, out var leaperComp);
        leaperComp.KnockdownDuration = entity.Comp.CollideKnockdown;
        Dirty(entity.Owner, leaperComp);

        args.Handled = true;
    }

    protected virtual void TryStunAndKnockdown(EntityUid uid, TimeSpan duration)
    {
        // Реализация на сервере
    }
}
