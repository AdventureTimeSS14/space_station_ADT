using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.ADT.Shadekin;
using Content.Shared.ADT.Silicon;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Systems.PickupHumans;

public sealed class PickupHumansSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickupHumansComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<PickupHumansComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PickupHumansComponent, PickupHumansDoAfterEvent>(OnPickupDoAfter);

        SubscribeLocalEvent<PickupingHumansComponent, ComponentStartup>(OnCarrierStartup);
        SubscribeLocalEvent<PickupingHumansComponent, ComponentShutdown>(OnCarrierShutdown);
        SubscribeLocalEvent<PickupingHumansComponent, RefreshMovementSpeedModifiersEvent>(OnCarrierRefreshSpeed);
        SubscribeLocalEvent<PickupingHumansComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PickupingHumansComponent, MobStateChangedEvent>(OnCarrierMobStateChanged);
        SubscribeLocalEvent<PickupingHumansComponent, DownedEvent>(OnCarrierDowned);
        SubscribeLocalEvent<PickupingHumansComponent, EntGotInsertedIntoContainerMessage>(OnCarrierInserted);
        SubscribeLocalEvent<PickupingHumansComponent, EntityTerminatingEvent>(OnCarrierTerminating);

        SubscribeLocalEvent<TakenHumansComponent, ComponentStartup>(OnCarriedStartup);
        SubscribeLocalEvent<TakenHumansComponent, ComponentShutdown>(OnCarriedShutdown);
        SubscribeLocalEvent<TakenHumansComponent, UpdateCanMoveEvent>(OnCarriedCanMove);
        SubscribeLocalEvent<TakenHumansComponent, StandAttemptEvent>(OnCarriedStandAttempt);
        SubscribeLocalEvent<TakenHumansComponent, PullAttemptEvent>(OnCarriedPullAttempt);
        SubscribeLocalEvent<TakenHumansComponent, InteractionAttemptEvent>(OnCarriedInteractionAttempt);
        SubscribeLocalEvent<TakenHumansComponent, ContainerGettingInsertedAttemptEvent>(OnCarriedInsertAttempt);
        SubscribeLocalEvent<TakenHumansComponent, BuckleAttemptEvent>(OnCarriedBuckleAttempt);
        SubscribeLocalEvent<TakenHumansComponent, StartClimbEvent>(OnCarriedStartClimb);
        SubscribeLocalEvent<TakenHumansComponent, ShadekinTeleportActionEvent>(OnCarriedTeleport);
        SubscribeLocalEvent<TakenHumansComponent, MoveInputEvent>(OnCarriedMoveInput);
        SubscribeLocalEvent<TakenHumansComponent, EscapingDoAfterEvent>(OnEscapeDoAfter);
        SubscribeLocalEvent<TakenHumansComponent, EntityTerminatingEvent>(OnCarriedTerminating);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.PickupHumans,
                InputCmdHandler.FromDelegate(ToggleReadyMode, handle: false))
            .Register<PickupHumansSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<PickupHumansSystem>();
    }

    public void DropCarriedBy(EntityUid carrier)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<PickupingHumansComponent>(carrier, out var carrierComp))
            return;

        if (!TryComp<TakenHumansComponent>(carrierComp.Carried, out var carriedComp))
        {
            RemComp<PickupingHumansComponent>(carrier);
            return;
        }

        Drop((carrierComp.Carried, carriedComp));
    }

    public void DropCarried(EntityUid carried)
    {
        if (!TryComp<TakenHumansComponent>(carried, out var carriedComp))
            return;

        Drop((carried, carriedComp));
    }

    public bool CanPickup(EntityUid carrier, EntityUid carried, bool showPopup)
    {
        if (carrier == carried)
        {
            if (showPopup)
                PopupTo(Loc.GetString("popup-attempt-interact-self"), carrier);

            return false;
        }

        if (!HasComp<PickupHumansComponent>(carrier) || !TryComp<PickupHumansComponent>(carried, out var carriedComp))
            return false;

        if (HasComp<PickupingHumansComponent>(carrier) || HasComp<TakenHumansComponent>(carrier))
            return false;

        if (HasComp<PickupingHumansComponent>(carried) || HasComp<TakenHumansComponent>(carried))
            return false;

        if (_container.IsEntityInContainer(carrier) || _container.IsEntityInContainer(carried))
            return false;

        if (_standing.IsDown(carrier))
            return false;

        if (!HasComp<MobIpcComponent>(carrier) && HasComp<MobIpcComponent>(carried))
        {
            if (showPopup)
                PopupTo(Loc.GetString("popup-pickup-attempt-ipc"), carrier);

            return false;
        }

        if (_hands.CountFreeHands(carrier) < carriedComp.HandsRequired)
        {
            if (showPopup)
                PopupTo(Loc.GetString("popup-hands-required"), carrier);

            return false;
        }

        return true;
    }

    private void ToggleReadyMode(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } uid)
            return;

        if (!TryComp<PickupHumansComponent>(uid, out var comp))
            return;

        SetReadyMode((uid, comp), !comp.InReadyPickupHumansMod);
    }

    private void SetReadyMode(Entity<PickupHumansComponent> ent, bool value)
    {
        if (ent.Comp.InReadyPickupHumansMod == value)
            return;

        ent.Comp.InReadyPickupHumansMod = value;
        Dirty(ent);

        if (value)
            _alerts.ShowAlert(ent.Owner, ent.Comp.PickupHumansAlert);
        else
            _alerts.ClearAlert(ent.Owner, ent.Comp.PickupHumansAlert);
    }

    private void OnGetVerbs(Entity<PickupHumansComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var target = ent.Owner;

        if (!CanPickup(user, target, false))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () =>
            {
                StartPickupDoAfter(user, target);
            },
            Text = Loc.GetString("verb-pickup"),
            Priority = 1,
        });
    }

    private void OnInteractHand(Entity<PickupHumansComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PickupHumansComponent>(args.User, out var userComp) || !userComp.InReadyPickupHumansMod)
            return;

        SetReadyMode((args.User, userComp), false);

        args.Handled = StartPickupDoAfter(args.User, ent.Owner);
    }

    private bool StartPickupDoAfter(EntityUid carrier, EntityUid carried)
    {
        if (!TryComp<PickupHumansComponent>(carried, out var carriedComp))
            return false;

        if (!CanPickup(carrier, carried, true))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, carrier, carriedComp.PickupTime, new PickupHumansDoAfterEvent(), carried, target: carried)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        PopupTo(Loc.GetString("popup-pickup-interact", ("target", carried)), carrier);
        PopupTo(Loc.GetString("popup-pickup-interact-target", ("user", carrier)), carried);

        return true;
    }

    private void OnPickupDoAfter(Entity<PickupHumansComponent> ent, ref PickupHumansDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = Pickup(args.Args.User, ent.Owner);
    }

    public bool Pickup(EntityUid carrier, EntityUid carried)
    {
        if (!CanPickup(carrier, carried, true))
            return false;

        if (_net.IsClient)
            return true;

        var handsRequired = Comp<PickupHumansComponent>(carried).HandsRequired;

        StopPulling(carrier);
        StopPulling(carried);

        if (TryComp<PullableComponent>(carried, out var pullable))
            _pulling.TryStopPull(carried, pullable);

        for (var i = 0; i < handsRequired; i++)
        {
            if (_virtualItem.TrySpawnVirtualItemInHand(carried, carrier))
                continue;

            _virtualItem.DeleteInHandsMatching(carrier, carried);
            return false;
        }

        var carriedComp = EnsureComp<TakenHumansComponent>(carried);
        carriedComp.Carrier = carrier;

        var carrierComp = EnsureComp<PickupingHumansComponent>(carrier);
        carrierComp.Carried = carried;
        Dirty(carrier, carrierComp);

        _standing.Down(carried, playSound: false, dropHeldItems: false);

        _transform.SetCoordinates(carried, new EntityCoordinates(carrier, Vector2.Zero));

        if (TryComp<PhysicsComponent>(carried, out var physics))
        {
            carriedComp.OriginalBodyType = physics.BodyType;
            _physics.SetBodyType(carried, BodyType.Static, body: physics);
        }

        Dirty(carried, carriedComp);

        _actionBlocker.UpdateCanMove(carried);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);

        return true;
    }

    private void PopupTo(string message, EntityUid recipient)
    {
        if (_net.IsClient)
            return;

        _popup.PopupEntity(message, recipient, recipient);
    }

    private void StopPulling(EntityUid uid)
    {
        if (!TryComp<PullerComponent>(uid, out var puller) || puller.Pulling is not { } pulling)
            return;

        if (TryComp<PullableComponent>(pulling, out var pullable))
            _pulling.TryStopPull(pulling, pullable, uid);
    }

    private void Drop(Entity<TakenHumansComponent> carried)
    {
        if (_net.IsClient)
            return;

        var carrier = carried.Comp.Carrier;
        var bodyType = carried.Comp.OriginalBodyType;

        _doAfter.Cancel(carried.Comp.EscapeDoAfter);

        RemComp<TakenHumansComponent>(carried);

        if (TryComp<PickupingHumansComponent>(carrier, out var carrierComp) && carrierComp.Carried == carried.Owner)
            RemComp<PickupingHumansComponent>(carrier);

        if (!TerminatingOrDeleted(carrier))
        {
            _virtualItem.DeleteInHandsMatching(carrier, carried);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);
        }

        if (TerminatingOrDeleted(carried))
            return;

        if (HasComp<PhysicsComponent>(carried))
            _physics.SetBodyType(carried, bodyType);

        _transform.AttachToGridOrMap(carried.Owner);

        _standing.Stand(carried);
        _actionBlocker.UpdateCanMove(carried);
    }

    private void OnVirtualItemDeleted(Entity<PickupingHumansComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != ent.Comp.Carried)
            return;

        DropCarriedBy(ent);
    }

    private void OnCarrierMobStateChanged(Entity<PickupingHumansComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        DropCarriedBy(ent);
    }

    private void OnCarrierDowned(Entity<PickupingHumansComponent> ent, ref DownedEvent args)
    {
        DropCarriedBy(ent);
    }

    private void OnCarrierInserted(Entity<PickupingHumansComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        DropCarriedBy(ent);
    }

    private void OnCarrierTerminating(Entity<PickupingHumansComponent> ent, ref EntityTerminatingEvent args)
    {
        DropCarriedBy(ent);
    }

    private void OnCarriedTerminating(Entity<TakenHumansComponent> ent, ref EntityTerminatingEvent args)
    {
        var carrier = ent.Comp.Carrier;

        if (!TryComp<PickupingHumansComponent>(carrier, out var carrierComp) || carrierComp.Carried != ent.Owner)
            return;

        RemComp<PickupingHumansComponent>(carrier);

        if (TerminatingOrDeleted(carrier))
            return;

        _virtualItem.DeleteInHandsMatching(carrier, ent.Owner);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);
    }

    private void OnCarriedBuckleAttempt(Entity<TakenHumansComponent> ent, ref BuckleAttemptEvent args)
    {
        Drop(ent);
    }

    private void OnCarriedStartClimb(Entity<TakenHumansComponent> ent, ref StartClimbEvent args)
    {
        Drop(ent);
    }

    private void OnCarriedTeleport(Entity<TakenHumansComponent> ent, ref ShadekinTeleportActionEvent args)
    {
        Drop(ent);
    }

    private void OnCarriedMoveInput(Entity<TakenHumansComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_doAfter.IsRunning(ent.Comp.EscapeDoAfter))
            return;

        if (!TryComp<PickupHumansComponent>(ent.Owner, out var comp))
            return;

        if (_container.IsEntityInContainer(ent.Comp.Carrier))
        {
            PopupTo(Loc.GetString("popup-drop-attempt-target"), ent.Owner);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, comp.PickupTime, new EscapingDoAfterEvent(), ent.Owner, target: ent.Comp.Carrier)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = false,
        };

        if (_doAfter.TryStartDoAfter(doAfter, out var id))
            ent.Comp.EscapeDoAfter = id;
    }

    private void OnEscapeDoAfter(Entity<TakenHumansComponent> ent, ref EscapingDoAfterEvent args)
    {
        ent.Comp.EscapeDoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        Drop(ent);
        args.Handled = true;
    }

    private void OnCarriedStartup(Entity<TakenHumansComponent> ent, ref ComponentStartup args)
    {
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    private void OnCarriedShutdown(Entity<TakenHumansComponent> ent, ref ComponentShutdown args)
    {
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    private void OnCarriedCanMove(Entity<TakenHumansComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        args.Cancel();
    }

    private void OnCarriedStandAttempt(Entity<TakenHumansComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        args.Cancel();
    }

    private void OnCarriedPullAttempt(Entity<TakenHumansComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnCarriedInteractionAttempt(Entity<TakenHumansComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Target == null || args.Target == ent.Comp.Carrier)
            return;

        args.Cancelled = true;
    }

    private void OnCarriedInsertAttempt(Entity<TakenHumansComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnCarrierStartup(Entity<PickupingHumansComponent> ent, ref ComponentStartup args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnCarrierShutdown(Entity<PickupingHumansComponent> ent, ref ComponentShutdown args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnCarrierRefreshSpeed(Entity<PickupingHumansComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        args.ModifySpeed(ent.Comp.WalkSpeedModifier, ent.Comp.SprintSpeedModifier);
    }
}

[Serializable, NetSerializable]
public sealed partial class PickupHumansDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class EscapingDoAfterEvent : SimpleDoAfterEvent
{
}
