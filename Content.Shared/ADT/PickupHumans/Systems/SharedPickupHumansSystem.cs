using Content.Shared.DoAfter;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
// using Content.Shared.Throwing; // TODO: Сделать небольшой бросок
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs;
using Content.Shared.Interaction.Events;
using Content.Shared.ADT.Crawling;
using Content.Shared.Popups;
using Content.Shared.Climbing.Events;
using Content.Shared.ADT.Silicon;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Verbs;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Systems.PickupHumans;

public abstract class SharedPickupHumansSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickupHumansComponent, GetVerbsEvent<AlternativeVerb>>(AddPickupVerb);
        SubscribeLocalEvent<PickupHumansComponent, PickupHumansDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PickupHumansComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<PickupHumansComponent, InteractHandEvent>(OnPickupInteract);
        SubscribeLocalEvent<PickupHumansComponent, ContainerGettingInsertedAttemptEvent>(OnContainerInsertAttempt);

        SubscribeLocalEvent<PickupingHumansComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PickupingHumansComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PickupingHumansComponent, DownAttemptEvent>(OnFallAttempt);

        SubscribeLocalEvent<TakenHumansComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<TakenHumansComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<TakenHumansComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<TakenHumansComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<TakenHumansComponent, StartClimbEvent>(OnStartClimb);

        SubscribeLocalEvent<PickupingHumansComponent, ComponentInit>(OnPickupSlowdownInit);
        SubscribeLocalEvent<PickupingHumansComponent, ComponentShutdown>(OnTargetSlowRemove);
        SubscribeLocalEvent<PickupingHumansComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshTargetMovespeed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.PickupHumans,
                InputCmdHandler.FromDelegate(SetInPickupHumansMod, handle: false))
            .Register<SharedPickupHumansSystem>();
    }

    private void SetInPickupHumansMod(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } uid)
            return;

        if (!TryComp<PickupHumansComponent>(uid, out var component))
            return;

        ShowAlertPickupHumans(session.AttachedEntity.Value, component);
    }

    private void ShowAlertPickupHumans(EntityUid uid, PickupHumansComponent component)
    {
        component.InReadyPickupHumansMod = !component.InReadyPickupHumansMod;

        if (component.InReadyPickupHumansMod)
            _alerts.ShowAlert(uid, component.PickupHumansAlert);
        else
            _alerts.ClearAlert(uid, component.PickupHumansAlert);

        Dirty(uid, component);
    }

    private void AddPickupVerb(EntityUid uid, PickupHumansComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (args.User == args.Target)
            return;

        if (_container.IsEntityInContainer(args.User) || _container.IsEntityInContainer(args.Target))
            return;

        if (!TryComp<HandsComponent>(uid, out var handComp))
            return;

        if (!TryComp<PickupHumansComponent>(args.Target, out var pickupHumansComponent))
            return;

        if (HasComp<PickupingHumansComponent>(args.User) || HasComp<TakenHumansComponent>(args.Target))
            return;

        if (HasComp<CrawlingComponent>(args.User))
            return;

        if (handComp.CountFreeHands() < component.HandsRequired || handComp.CountFreeHands() > component.HandsRequired)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartPickupDoAfter(args.User, args.Target, pickupHumansComponent);
            },
            Text = Loc.GetString("verb-pickup"),
            Priority = 1
        };
        args.Verbs.Add(verb);
    }

    private void OnPickupInteract(EntityUid uid, PickupHumansComponent component, InteractHandEvent args)
    {
        if (!HasComp<MobIpcComponent>(args.User) && HasComp<MobIpcComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("popup-pickup-attempt-ipc"), args.User, args.User);
            return;
        }

        if (HasComp<TakenHumansComponent>(args.User))
            return;

        if (!TryComp<PickupHumansComponent>(args.User, out var userComponent))
            return;

        if (!userComponent.InReadyPickupHumansMod)
            return;

        userComponent.InReadyPickupHumansMod = false;
        _alerts.ClearAlert(args.User, userComponent.PickupHumansAlert);

        StartPickupDoAfter(args.User, args.Target, userComponent);
    }

    private void StartPickupDoAfter(EntityUid uid, EntityUid target, PickupHumansComponent comp)
    {
        if (HasComp<CrawlingComponent>(uid))
            return;

        if (HasComp<PickupingHumansComponent>(target))
            return;

        _popup.PopupEntity(Loc.GetString("popup-pickup-interact", ("target", target)), uid, uid);

        _popup.PopupEntity(Loc.GetString("popup-pickup-interact-target", ("user", uid)), target, target);

        var ev = new PickupHumansDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, uid, comp.PickupTime, ev, target, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, PickupHumansComponent component, PickupHumansDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!CanPickup(args.Args.User, uid, component))
            return;

        Pickup(args.Args.User, uid);

        args.Handled = true;
    }

    private void Pickup(EntityUid uid, EntityUid target)
    {
        if (!TryComp<PickupHumansComponent>(target, out var userComponent))
            return;

        if (TryComp<PullerComponent>(target, out var pullerComp) && pullerComp.Pulling != EntityUid.Invalid &&
        TryComp<PullableComponent>(pullerComp.Pulling, out var pullableComp))
        {
            _pulling.TryStopPull(pullerComp.Pulling!.Value, pullableComp, target);
        }

        _standing.Down(target, dropHeldItems: false);

        _transform.AttachToGridOrMap(uid);
        _transform.AttachToGridOrMap(target);
        _transform.SetCoordinates(target, Transform(uid).Coordinates);
        _transform.SetParent(target, uid);

        for (var i = 0; i < 2; i++)
        {
            _virtualItemSystem.TrySpawnVirtualItemInHand(target, uid);
        }

        var pickupingHumansComponent = EnsureComp<PickupingHumansComponent>(uid);
        var takenHumansComponent = EnsureComp<TakenHumansComponent>(target);

        EnsureComp<KnockedDownComponent>(target);

        takenHumansComponent.Target = target;
        userComponent.User = uid;

        PickupHumansComponent.Target = target;

        pickupingHumansComponent.User = uid;

        _actionBlocker.UpdateCanMove(target);
    }

    public void DropFromHands(EntityUid uid, EntityUid target)
    {
        RemComp<PickupingHumansComponent>(uid);
        RemComp<TakenHumansComponent>(target);
        RemComp<KnockedDownComponent>(target);

        _standing.Stand(target);

        _virtualItemSystem.DeleteInHandsMatching(uid, target);

        _transform.AttachToGridOrMap(target);
        _actionBlocker.UpdateCanMove(target);

        _hands.TryDrop(uid, target);
    }

    private bool CanPickup(EntityUid uid, EntityUid target, PickupHumansComponent? pickupComp)
    {
        if (uid == target)
        {
            _popup.PopupEntity(Loc.GetString("popup-attempt-interact-self"), uid, uid);
            return false;
        }
        if (!Resolve(target, ref pickupComp, false))
            return false;

        if (!TryComp<HandsComponent>(uid, out var handComp))
            return false;

        if (_container.IsEntityInContainer(uid) || _container.IsEntityInContainer(target))
            return false;

        if (!HasComp<PickupHumansComponent>(uid) || !HasComp<PickupHumansComponent>(target))
            return false;

        if (handComp.CountFreeHands() != pickupComp.HandsRequired)
        {
            _popup.PopupEntity(Loc.GetString("popup-hands-required"), uid, uid);
            return false;
        }
        return true;
    }

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

        if (_container.IsEntityInContainer(uid))
        {
            _popup.PopupEntity(Loc.GetString("popup-down-attempt"), uid, uid);
            args.Cancel();
        }
    }
    private void OnVirtualItemDeleted(EntityUid uid, PickupingHumansComponent component, VirtualItemDeletedEvent args)
    {
        if (!HasComp<TakenHumansComponent>(args.BlockingEntity) || !HasComp<PickupHumansComponent>(args.BlockingEntity))
            return;

        DropFromHands(uid, args.BlockingEntity);
    }

    private void OnPickupSlowdownInit(EntityUid uid, PickupingHumansComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnTargetSlowRemove(EntityUid uid, PickupingHumansComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshTargetMovespeed(EntityUid uid, PickupingHumansComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
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

    private void OnMobStateChanged(EntityUid uid, PickupingHumansComponent component, MobStateChangedEvent args)
    {
        DropFromHands(uid, component.User);
    }

    private void OnMoveAttempt(EntityUid uid, TakenHumansComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnStandAttempt(EntityUid uid, TakenHumansComponent component, StandAttemptEvent args)
    {
        args.Cancel();
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
