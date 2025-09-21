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
using Content.Shared.ADT.Shadekin;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs;
using Content.Shared.ADT.Crawling;
using Content.Shared.Popups;
using Content.Shared.ADT.Silicon;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Verbs;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Containers;
using Content.Shared.Buckle.Components;
using Content.Shared.Weapons.Melee;

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
        SubscribeLocalEvent<PickupHumansComponent, InteractHandEvent>(OnPickupInteract);
        SubscribeLocalEvent<PickupHumansComponent, ShadekinTeleportActionEvent>(OnTeleportShadekin);

        SubscribeLocalEvent<PickupingHumansComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PickupingHumansComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<TakenHumansComponent, BuckleAttemptEvent>(OnBuckle);

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

    /// <summary>
    /// Показывает alert для сущности
    /// </summary>
    private void ShowAlertPickupHumans(EntityUid uid, PickupHumansComponent component)
    {
        component.InReadyPickupHumansMod = !component.InReadyPickupHumansMod;

        if (component.InReadyPickupHumansMod)
            _alerts.ShowAlert(uid, component.PickupHumansAlert);
        else
            _alerts.ClearAlert(uid, component.PickupHumansAlert);

        Dirty(uid, component);
    }

    /// <summary>
    /// Добавления verb'a взятия на руки
    /// </summary>
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

    /// <summary>
    /// Реагирует на дотрагивание до сущности в режиме готовности поднять её
    /// </summary>
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


    /// <summary>
    /// Включает DoAfter поднятия
    /// </summary>
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


    /// <summary>
    /// Поднимает сущность на руки
    /// </summary>
    private void Pickup(EntityUid uid, EntityUid target)
    {
        if (!TryComp<PickupHumansComponent>(target, out var userComponent))
            return;

        if (TryComp<PullerComponent>(target, out var pullerComp) && pullerComp.Pulling != EntityUid.Invalid &&
        TryComp<PullableComponent>(pullerComp.Pulling, out var pullableComp))
        {
            _pulling.TryStopPull(pullerComp.Pulling!.Value, pullableComp, target);
        }

        var meleeWeaponCompUid = EnsureComp<MeleeWeaponComponent>(uid);
        var meleeWeaponCompTarget = EnsureComp<MeleeWeaponComponent>(target);

        meleeWeaponCompTarget.AltDisarm = false;
        meleeWeaponCompUid.AltDisarm = false;

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
        userComponent.Target = target;

        pickupingHumansComponent.User = uid;

        _actionBlocker.UpdateCanMove(target);
    }

    /// <summary>
    /// Реагирует на выпадение сущности из рук
    /// </summary>
    public void DropFromHands(EntityUid uid, EntityUid target)
    {
        RemComp<PickupingHumansComponent>(uid);
        RemComp<TakenHumansComponent>(target);
        RemComp<KnockedDownComponent>(target);


        var meleeWeaponCompUid = EnsureComp<MeleeWeaponComponent>(uid);
        var meleeWeaponCompTarget = EnsureComp<MeleeWeaponComponent>(target);

        meleeWeaponCompTarget.AltDisarm = true;
        meleeWeaponCompUid.AltDisarm = true;

        _standing.Stand(target);

        _virtualItemSystem.DeleteInHandsMatching(uid, target);

        _transform.AttachToGridOrMap(target);
        _actionBlocker.UpdateCanMove(target);

        _hands.TryDrop(uid, target);
    }

    private bool CanPickup(EntityUid uid, EntityUid target, PickupHumansComponent? pickupComp)
    {
        if (HasComp<CrawlingComponent>(uid))
            return false;

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

    private void OnVirtualItemDeleted(EntityUid uid, PickupingHumansComponent component, VirtualItemDeletedEvent args)
    {
        if (!HasComp<TakenHumansComponent>(args.BlockingEntity) || !HasComp<PickupHumansComponent>(args.BlockingEntity))
            return;

        DropFromHands(uid, args.BlockingEntity);
    }

    private void OnBuckle(EntityUid uid, TakenHumansComponent component, ref BuckleAttemptEvent args)
    {
        if (!TryComp<PickupHumansComponent>(uid, out var pickupHumansComponent))
            return;

        DropFromHands(pickupHumansComponent.User, pickupHumansComponent.Target);
    }


    private void OnTeleportShadekin(EntityUid uid, PickupHumansComponent comp, ShadekinTeleportActionEvent args)
    {
        if (comp.User != EntityUid.Invalid)
        {
            DropFromHands(comp.User, uid);
        }
    }


    /// <summary>
    /// Реагирует на поднятие на руки и вызывает другую функцию для обновления скорости
    /// </summary>
    private void OnPickupSlowdownInit(EntityUid uid, PickupingHumansComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Реагирует на опускание сущности и обновляет скорость до дефолтной
    /// </summary>
    private void OnTargetSlowRemove(EntityUid uid, PickupingHumansComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Обновляет скорость носителя при взятии на руки
    /// </summary>
    private void OnRefreshTargetMovespeed(EntityUid uid, PickupingHumansComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnMobStateChanged(EntityUid uid, PickupingHumansComponent component, MobStateChangedEvent args)
    {
        DropFromHands(uid, component.User);
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
