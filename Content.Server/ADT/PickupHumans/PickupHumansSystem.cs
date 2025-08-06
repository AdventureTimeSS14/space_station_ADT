using Content.Shared.ADT.Systems.PickupHumans;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.Movement.Events;
using Robust.Shared.Containers;



namespace Content.Server.ADT.PickupHumans;

public sealed partial class PickupHumansSystem : SharedPickupHumansSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TakenHumansComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<TakenHumansComponent, EscapingDoAfterEvent>(OnEscape);
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
}