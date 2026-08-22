using Content.Shared.ADT.Alert.Click;
using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.OfferItem;

public abstract partial class SharedOfferItemSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected static readonly ProtoId<AlertPrototype> OfferAlert = "Offer";

    public override void Initialize()
    {
        SubscribeLocalEvent<OfferItemComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OfferItemComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<OfferItemComponent, DidUnequipHandEvent>(OnDidUnequipHand);
        SubscribeLocalEvent<OfferItemComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<OfferItemComponent, AcceptOfferAlertEvent>(OnAcceptAlert);

        InitializeInteractions();
    }

    private void OnInteractUsing(Entity<OfferItemComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.User == ent.Owner)
            return;

        if (!TryComp<OfferItemComponent>(args.User, out var offer) || !offer.IsInOfferMode)
            return;

        if (ent.Comp.IsInReceiveMode)
            return;

        if (offer.Item is not { } item || item != args.Used)
            return;

        if (offer.Target is not null)
            Cancel((args.User, offer), popup: false);

        offer.IsInOfferMode = false;
        offer.Target = ent.Owner;
        Dirty(args.User, offer);

        SetReceiveMode(ent, true, args.User);

        PopupTo(Loc.GetString("offer-item-try-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(ent, EntityManager))), args.User, args.User);

        PopupTo(Loc.GetString("offer-item-try-give-target",
            ("user", Identity.Entity(args.User, EntityManager)),
            ("item", Identity.Entity(item, EntityManager))), args.User, ent.Owner);

        args.Handled = true;
    }

    private void OnAcceptAlert(Entity<OfferItemComponent> ent, ref AcceptOfferAlertEvent args)
    {
        if (args.Handled || args.AlertId != OfferAlert)
            return;

        args.Handled = TryReceive(ent);
    }

    private void OnDidUnequipHand(Entity<OfferItemComponent> ent, ref DidUnequipHandEvent args)
    {
        if (ent.Comp.Item != args.Unequipped)
            return;

        Cancel(ent);
    }

    private void OnShutdown(Entity<OfferItemComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Target is { } uid && TryComp<OfferItemComponent>(uid, out var partner) && partner.Target == ent.Owner)
            Reset((uid, partner));
    }

    private void OnMove(EntityUid uid, OfferItemComponent component, MoveEvent args)
    {
        if (component.Target is not { } target)
            return;

        if (_transform.InRange(args.NewPosition, Transform(target).Coordinates, component.MaxOfferDistance))
            return;

        Cancel((uid, component));
    }

    public bool TryReceive(Entity<OfferItemComponent> ent)
    {
        if (!ent.Comp.IsInReceiveMode || ent.Comp.Target is not { } giverUid)
            return false;

        if (!_actionBlocker.CanInteract(ent.Owner, null))
            return false;

        if (!_transform.InRange(Transform(ent).Coordinates, Transform(giverUid).Coordinates, ent.Comp.MaxOfferDistance))
        {
            Cancel(ent);
            return false;
        }

        if (!TryComp<OfferItemComponent>(giverUid, out var giverComp) || giverComp.Item is not { } item)
        {
            Cancel(ent, popup: false);
            return false;
        }

        if (!TryComp<HandsComponent>(ent, out var hands))
            return false;

        giverComp.Item = null;

        if (!_hand.TryPickup(ent, item, handsComp: hands))
        {
            giverComp.Item = item;
            PopupTo(Loc.GetString("offer-item-full-hand"), ent, ent);
            return false;
        }

        PopupTo(Loc.GetString("offer-item-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(ent, EntityManager))), giverUid, giverUid);

        PopupTo(Loc.GetString("offer-item-give-target",
            ("user", Identity.Entity(giverUid, EntityManager)),
            ("item", Identity.Entity(item, EntityManager))), ent, ent);

        Reset((giverUid, giverComp));
        Reset(ent);
        return true;
    }

    public void Cancel(Entity<OfferItemComponent> ent, bool popup = true)
    {
        Entity<OfferItemComponent>? partner = null;
        if (ent.Comp.Target is { } partnerUid && TryComp<OfferItemComponent>(partnerUid, out var partnerComp))
            partner = (partnerUid, partnerComp);

        if (popup && partner is { } other)
        {
            var giver = ent.Comp.Item != null ? ent : other;
            var receiver = giver.Owner == ent.Owner ? other : ent;

            if (giver.Comp.Item is { } offered)
            {
                PopupTo(Loc.GetString("offer-item-no-give",
                    ("item", Identity.Entity(offered, EntityManager)),
                    ("target", Identity.Entity(receiver, EntityManager))), giver, giver);

                PopupTo(Loc.GetString("offer-item-no-give-target",
                    ("user", Identity.Entity(giver, EntityManager)),
                    ("item", Identity.Entity(offered, EntityManager))), giver, receiver);
            }
        }

        Reset(ent);

        if (partner is { } partnerEnt && partnerEnt.Comp.Target == ent.Owner)
            Reset(partnerEnt);
    }

    private void SetReceiveMode(Entity<OfferItemComponent> ent, bool value, EntityUid? target)
    {
        ent.Comp.IsInReceiveMode = value;
        ent.Comp.Target = target;

        if (value)
            _alerts.ShowAlert(ent.Owner, OfferAlert);
        else
            _alerts.ClearAlert(ent.Owner, OfferAlert);

        Dirty(ent);
    }

    private void Reset(Entity<OfferItemComponent> ent)
    {
        if (ent.Comp.IsInReceiveMode)
            _alerts.ClearAlert(ent.Owner, OfferAlert);

        ent.Comp.IsInOfferMode = false;
        ent.Comp.IsInReceiveMode = false;
        ent.Comp.Hand = null;
        ent.Comp.Item = null;
        ent.Comp.Target = null;

        Dirty(ent);
    }

    private void PopupTo(string message, EntityUid uid, EntityUid recipient)
    {
        if (_net.IsClient)
            return;

        _popup.PopupEntity(message, uid, recipient);
    }

    protected bool IsInOfferMode(EntityUid? entity, OfferItemComponent? component = null)
    {
        return entity is not null && Resolve(entity.Value, ref component, false) && component.IsInOfferMode;
    }
}
