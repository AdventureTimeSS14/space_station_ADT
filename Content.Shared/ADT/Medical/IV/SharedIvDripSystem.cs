using System.Linq;
using Content.Shared.ADT.Silicon;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Ghost;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Medical.IV;

public abstract class SharedIvDripSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IVDripComponent, EntInsertedIntoContainerMessage>(OnIVDripEntInserted);
        SubscribeLocalEvent<IVDripComponent, GetVerbsEvent<AlternativeVerb>>(AddSetTransferAmountIVDripVerbs);
        SubscribeLocalEvent<IVDripComponent, EntRemovedFromContainerMessage>(OnIVDripEntRemoved);
        SubscribeLocalEvent<IVDripComponent, AfterAutoHandleStateEvent>(OnIVDripAfterHandleState);
        SubscribeLocalEvent<IVDripComponent, EntityUnpausedEvent>(OnIVDripUnPaused);

        SubscribeLocalEvent<IVDripComponent, CanDragEvent>(OnIVDripCanDrag);
        SubscribeLocalEvent<IVDripComponent, CanDropDraggedEvent>(OnIVDripCanDropDragged);
        SubscribeLocalEvent<IVDripComponent, DragDropDraggedEvent>(OnIVDripDragDropDragged);
        SubscribeLocalEvent<IVDripComponent, InteractHandEvent>(OnIVInteractHand);
        SubscribeLocalEvent<IVDripComponent, GetVerbsEvent<InteractionVerb>>(OnIVVerbs);

        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        SubscribeLocalEvent<BloodstreamComponent, CanDropTargetEvent>(OnMarineCanDropTarget);

        SubscribeLocalEvent<BloodPackComponent, MapInitEvent>(OnBloodPackMapInitEvent);
        SubscribeLocalEvent<BloodPackComponent, SolutionChangedEvent>(OnBloodPackSolutionChanged);
    }

    private void AddSetTransferAmountIVDripVerbs(Entity<IVDripComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (ent.Comp.TransferAmounts.Count <= 1)
            return;

        var user = args.User;

        var min = ent.Comp.TransferAmounts.Min();
        var max = ent.Comp.TransferAmounts.Max();
        var cur = ent.Comp.CurrentTransferAmount;
        var toggleAmount = cur == max ? min : max;

        var priority = 0;
        AlternativeVerb toggleVerb = new()
        {
            Text = Loc.GetString("comp-solution-transfer-verb-toggle", ("amount", toggleAmount)),
            Category = VerbCategory.SetTransferAmount,
            Act = () =>
            {
                ent.Comp.CurrentTransferAmount = toggleAmount;
                _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", toggleAmount)), user, user);
                Dirty(ent);
            },

            Priority = priority
        };
        args.Verbs.Add(toggleVerb);

        priority -= 1;

        foreach (var amount in ent.Comp.TransferAmounts)
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("comp-solution-transfer-verb-amount", ("amount", amount)),
                Category = VerbCategory.SetTransferAmount,
                Act = () =>
                {
                    ent.Comp.CurrentTransferAmount = amount;
                    _popup.PopupClient(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)), user, user);
                    Dirty(ent);
                },

                Priority = priority
            };

            priority -= 1;

            args.Verbs.Add(verb);
        }
    }

    private void OnIVDripEntInserted(Entity<IVDripComponent> iv, ref EntInsertedIntoContainerMessage args)
    {
        UpdateIVVisuals(iv);
    }

    private void OnIVDripEntRemoved(Entity<IVDripComponent> iv, ref EntRemovedFromContainerMessage args)
    {
        UpdateIVVisuals(iv);
    }

    private void OnIVDripAfterHandleState(Entity<IVDripComponent> iv, ref AfterAutoHandleStateEvent args)
    {
        UpdateIVAppearance(iv);
    }

    private void OnIVDripUnPaused(Entity<IVDripComponent> iv, ref EntityUnpausedEvent args)
    {
        iv.Comp.TransferAt += args.PausedTime;
    }

    private void OnIVDripCanDrag(Entity<IVDripComponent> iv, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnIVDripCanDropDragged(Entity<IVDripComponent> iv, ref CanDropDraggedEvent args)
    {
        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        if (HasComp<BloodstreamComponent>(args.Target) && InRange(iv, args.Target))
        {
            args.Handled = true;
            args.CanDrop = true;
        }
    }

    // TODO CM14 check for BloodstreamComponent instead of MarineComponent
    private void OnMarineCanDropTarget(Entity<BloodstreamComponent> marine, ref CanDropTargetEvent args)
    {
        var iv = args.Dragged;
        if (TryComp(iv, out IVDripComponent? ivComp) && InRange((iv, ivComp), marine))
        {
            args.Handled = true;
            args.CanDrop = true;
        }
    }

    private void OnIVDripDragDropDragged(Entity<IVDripComponent> iv, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        if (iv.Comp.AttachedTo is null)
            Attach(iv, args.User, args.Target);
        else
            Detach(iv, false, true);
    }

    private void OnIVInteractHand(Entity<IVDripComponent> iv, ref InteractHandEvent args)
    {
        Detach(iv, false, true);
    }

    private void OnIVVerbs(Entity<IVDripComponent> iv, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (HasComp<GhostComponent>(args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => ToggleInject(iv, user),
            Text = Loc.GetString("cm-iv-verb-toggle-inject")
        });
    }

    private void OnBloodPackMapInitEvent(Entity<BloodPackComponent> pack, ref MapInitEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(pack, out var solMan))
            return;

        if (!_solutionContainer.TryGetSolution(solMan, pack.Comp.Solution, out var packSolution))
            return;

        UpdatePackVisuals(pack, packSolution);
    }

    private void OnBloodPackSolutionChanged(Entity<BloodPackComponent> pack, ref SolutionChangedEvent args)
    {
        UpdatePackVisuals(pack, args.Solution.Comp.Solution);
    }

    protected bool InRange(Entity<IVDripComponent> iv, EntityUid to)
    {
        var ivPos = _transform.GetMapCoordinates(iv);
        var toPos = _transform.GetMapCoordinates(to);
        return ivPos.InRange(toPos, iv.Comp.Range);
    }

    protected void Attach(Entity<IVDripComponent> iv, EntityUid user, EntityUid to)
    {
        if (HasComp<MobIpcComponent>(to))
            return;

        if (!InRange(iv, to))
            return;

        iv.Comp.AttachedTo = to;
        Dirty(iv);

        if (!_timing.IsFirstTimePredicted)
            return;

        _popup.PopupClient(Loc.GetString("cm-iv-attach-self", ("target", to)), to, user);

        var others = Filter.PvsExcept(user);
        _popup.PopupEntity(Loc.GetString("cm-iv-attach-self", ("target", others)), to, others, true);
    }

    protected void Detach(Entity<IVDripComponent> iv, bool rip, bool predict)
    {
        if (iv.Comp.AttachedTo is not { } target)
            return;

        iv.Comp.AttachedTo = null;
        Dirty(iv);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (rip)
        {
            if (iv.Comp.RipDamage != null)
                _damageable.TryChangeDamage(target, iv.Comp.RipDamage, true);

            var message = Loc.GetString("cm-iv-rip", ("target", target));
            if (predict)
            {
                _popup.PopupClient(message, target, target);

                _popup.PopupEntity(message, target);
            }
            else
            {
                _popup.PopupEntity(message, target);
            }
        }
        else
        {
            var selfMessage = Loc.GetString("cm-iv-detach-self", ("target", target));
            if (predict)
                _popup.PopupClient(selfMessage, target, target);
            else
                _popup.PopupEntity(selfMessage, target);

            _popup.PopupEntity(Loc.GetString("cm-iv-detach-others", ("target", target)), target);
        }
    }

    private void ToggleInject(Entity<IVDripComponent> iv, EntityUid user)
    {
        iv.Comp.Injecting = !iv.Comp.Injecting;
        Dirty(iv);

        var msg = iv.Comp.Injecting
            ? Loc.GetString("cm-iv-now-injecting")
            : Loc.GetString("cm-iv-now-taking");

        _popup.PopupClient(msg, iv, user);
    }

    private void UpdatePackVisuals(Entity<BloodPackComponent> pack, Solution solution)
    {
        if (_containers.TryGetContainingContainer(pack.Owner, out var container) &&
            TryComp(container.Owner, out IVDripComponent? iv))
        {
            iv.FillColor = solution.GetColor(_prototype);
            iv.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
            Dirty(container.Owner, iv);
            UpdateIVAppearance((container.Owner, iv));
        }

        UpdatePackAppearance(pack);
    }

    private void UpdateIVVisuals(Entity<IVDripComponent> iv)
    {
        // the client doesn't always know about solutions
        if (_net.IsClient)
        {
            UpdateIVAppearance(iv);
            return;
        }

        if (_containers.TryGetContainer(iv, iv.Comp.Slot, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp(entity, out BloodPackComponent? pack) &&
                    _solutionContainer.TryGetSolution(entity, pack.Solution, out var solEnt))
                {
                    var solution = solEnt.Value.Comp.Solution;

                    iv.Comp.FillColor = solution.GetColor(_prototype);
                    iv.Comp.FillPercentage = (int)(solution.Volume / solution.MaxVolume * 100);
                    Dirty(iv);
                    UpdateIVAppearance(iv);
                    return;
                }
            }

            iv.Comp.FillColor = Color.White;
            iv.Comp.FillPercentage = 0;
            Dirty(iv);
            UpdateIVAppearance(iv);
        }
    }

    protected virtual void UpdateIVAppearance(Entity<IVDripComponent> iv)
    {
    }

    protected virtual void UpdatePackAppearance(Entity<BloodPackComponent> pack)
    {
    }
}
