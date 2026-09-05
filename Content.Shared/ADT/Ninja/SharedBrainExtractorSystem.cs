using Content.Shared.ADT.Ninja.Components;
using Content.Shared.Body;
using Content.Shared.Climbing.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Ninja;

public abstract class SharedBrainExtractorSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainExtractorPodComponent, ComponentInit>(OnPodInit);
        SubscribeLocalEvent<BrainExtractorPodComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<BrainExtractorPodComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<BrainExtractorPodComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<BrainExtractorPodComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<BrainExtractorPodComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
    }

    private void OnPodInit(EntityUid uid, BrainExtractorPodComponent comp, ComponentInit args)
    {
        comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "brain-extractor-bodyContainer");
    }

    private void OnCanDrop(EntityUid uid, BrainExtractorPodComponent comp, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= CanInsert(uid, args.Dragged, comp);
    }

    private void OnDragDrop(EntityUid uid, BrainExtractorPodComponent comp, ref DragDropTargetEvent args)
    {
        if (!CanInsert(uid, args.Dragged, comp))
            return;

        args.Handled = true;
        Insert(uid, args.Dragged, comp);
    }

    private void OnRelayMovement(EntityUid uid, BrainExtractorPodComponent comp, ref ContainerRelayMovementEntityEvent args)
    {
        if (comp.BodyContainer.ContainedEntity == args.Entity)
            _container.Remove(args.Entity, comp.BodyContainer);
    }

    private void OnGetInteractionVerbs(EntityUid uid, BrainExtractorPodComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using == null || !args.CanAccess || !args.CanInteract || IsOccupied(comp) || !CanInsert(uid, args.Using.Value, comp))
            return;

        var name = MetaData(args.Using.Value).EntityName;
        InteractionVerb verb = new()
        {
            Act = () => Insert(uid, args.Using.Value, comp),
            Category = VerbCategory.Insert,
            Text = name
        };
        args.Verbs.Add(verb);
    }

    private void OnGetAlternativeVerbs(EntityUid uid, BrainExtractorPodComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (IsOccupied(comp))
        {
            AlternativeVerb verb = new()
            {
                Act = () => Remove(uid, comp),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant"),
                Priority = 1
            };
            args.Verbs.Add(verb);
        }

        if (!IsOccupied(comp) && CanInsert(uid, args.User, comp))
        {
            AlternativeVerb verb = new()
            {
                Act = () => Insert(uid, args.User, comp),
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(verb);
        }
    }

    private bool CanInsert(EntityUid uid, EntityUid target, BrainExtractorPodComponent comp)
    {
        if (comp.BodyContainer == null)
            return false;
        if (IsOccupied(comp))
            return false;
        return HasComp<BodyComponent>(target);
    }

    private bool IsOccupied(BrainExtractorPodComponent comp)
    {
        return comp.BodyContainer != null && comp.BodyContainer.ContainedEntity != null;
    }

    private void Insert(EntityUid uid, EntityUid target, BrainExtractorPodComponent comp)
    {
        if (comp.BodyContainer == null || IsOccupied(comp))
            return;
        if (!HasComp<BodyComponent>(target))
            return;
        _container.Insert(target, comp.BodyContainer);
    }

    private void Remove(EntityUid uid, BrainExtractorPodComponent comp)
    {
        if (comp.BodyContainer == null)
            return;
        if (comp.BodyContainer.ContainedEntity is not { Valid: true } contained)
            return;
        _container.Remove(contained, comp.BodyContainer);
        _climb.ForciblySetClimbing(contained, uid);
    }
}

public sealed class BrainExtractorPodSharedSystem : SharedBrainExtractorSystem
{
}
