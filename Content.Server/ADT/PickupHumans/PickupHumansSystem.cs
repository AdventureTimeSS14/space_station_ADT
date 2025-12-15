using Content.Shared.ADT.Systems.PickupHumans;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.Movement.Events;
using Robust.Shared.Containers;
using Content.Shared.Standing;
using Content.Shared.Hands;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Climbing.Events;
using Content.Shared.ADT.Spike;
using Robust.Shared.Random;
using Content.Shared.Buckle.Components;

namespace Content.Server.ADT.PickupHumans;

public sealed partial class PickupHumansSystem : SharedPickupHumansSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TakenHumansComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<TakenHumansComponent, EscapingDoAfterEvent>(OnEscape);

        SubscribeLocalEvent<TakenHumansComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<TakenHumansComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<TakenHumansComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<TakenHumansComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<TakenHumansComponent, StartClimbEvent>(OnStartClimb);

        SubscribeLocalEvent<PickupingHumansComponent, DownAttemptEvent>(OnFallAttempt);
        SubscribeLocalEvent<PickupHumansComponent, ContainerGettingInsertedAttemptEvent>(OnContainerInsertAttempt);
        SubscribeLocalEvent<PickupHumansComponent, DropAttemptEvent>(OnDropAttempt);
    }

    private void OnMoveInput(EntityUid uid, TakenHumansComponent component, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (!TryComp<PickupHumansComponent>(uid, out var pickupComponent))
            return;

        var user = pickupComponent.User;

        AttemptEscape(user, uid, pickupComponent);
    }

    private void AttemptEscape(EntityUid uid, EntityUid target, PickupHumansComponent component)
    {
        if (_containerSystem.IsEntityInContainer(uid))
        {
            _popup.PopupEntity(Loc.GetString("popup-drop-attempt-target"), target, target);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, component.PickupTime, new EscapingDoAfterEvent(), target, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnEscape(EntityUid uid, TakenHumansComponent component, EscapingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        DropFromHands(args.User, component.Target);
    }

    #region Проверка на взаимодействия носителя и переносимого
    private void OnInteractionAttempt(EntityUid uid, TakenHumansComponent component, InteractionAttemptEvent args)
    {
        if (TryComp<PickupingHumansComponent>(args.Target, out var pickupingHumansComponent) && pickupingHumansComponent.User == args.Target)
            return;

        args.Cancelled = true;
    }

    private void OnDropAttempt(EntityUid uid, PickupHumansComponent component, DropAttemptEvent args)
    {
        if (!HasComp<PickupingHumansComponent>(uid))
            return;

        if (_containerSystem.IsEntityInContainer(uid))
        {
            _popup.PopupEntity(Loc.GetString("popup-down-attempt"), uid, uid);
            args.Cancel();
        }
    }

    private void OnFallAttempt(EntityUid uid, PickupingHumansComponent component, DownAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("pickup-cannot-down-while-pickuping"), uid, uid);
        args.Cancel();
    }

    private void OnContainerInsertAttempt(EntityUid uid, PickupHumansComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        if (!HasComp<TakenHumansComponent>(uid) || HasComp<PickupingHumansComponent>(uid))
            return;

        args.Cancel();
    }

    private void OnStartClimb(EntityUid uid, TakenHumansComponent component, ref StartClimbEvent args)
    {
        if (!TryComp<PickupHumansComponent>(uid, out var pickupHumansComponent))
            return;

        DropFromHands(pickupHumansComponent.User, uid);
    }

    private void OnPullAttempt(EntityUid uid, TakenHumansComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMoveAttempt(EntityUid uid, TakenHumansComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnStandAttempt(EntityUid uid, TakenHumansComponent component, StandAttemptEvent args)
    {
        args.Cancel();
    }
    #endregion

    // SpikeSystem: метод, позволяющий снять человека на крюке с некоторой вероятностью в SpikeComponent
    public override bool ShouldAllowPickup(EntityUid user, EntityUid target, PickupHumansComponent component)
    {
        if (SharedSpikeSystem.IsEntityImpaled(user, EntityManager))
            return false;

        if (SharedSpikeSystem.IsEntityImpaled(target, EntityManager))
        {
            if (!TryComp<BuckleComponent>(target, out var buckle) || !buckle.Buckled || buckle.BuckledTo == null)
            {
                return false;
            }

            if (!TryComp<SpikeComponent>(buckle.BuckledTo.Value, out var spikeComp))
            {
                return false;
            }

            if (!_random.Prob(spikeComp.PickupChance))
            {
                _popup.PopupEntity(Loc.GetString("spike-pickup-failed"), user, user);
                _popup.PopupEntity(Loc.GetString("spike-pickup-failed-target"), target, target);
                return false;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("spike-pickup-success"), user, user);
                _popup.PopupEntity(Loc.GetString("spike-pickup-success-target"), target, target);
                return true;
            }
        }

        return true;
    }
}
