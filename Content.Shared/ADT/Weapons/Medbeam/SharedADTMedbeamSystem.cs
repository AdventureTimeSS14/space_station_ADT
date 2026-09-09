using Content.Shared.ADT.Heretic.Common;
using Content.Shared.Damage.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.Medbeam;

public abstract partial class SharedADTMedbeamSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public const string BeamId = "medbeam";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMedbeamComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTMedbeamComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ADTMedbeamComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<ADTMedbeamComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<ADTMedbeamComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
    }

    private void OnAfterInteract(Entity<ADTMedbeamComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (ent.Comp.RequireMech)
        {
            if (GetHolder(ent) is not { } holder || !HasComp<MechComponent>(holder))
                return;
        }

        DetachBeam(ent);

        if (args.Target is not { } target || target == args.User || !IsValidTarget(target))
        {
            args.Handled = true;
            return;
        }

        AttachBeam(ent, target);
        args.Handled = true;
    }

    protected virtual bool IsValidTarget(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return false;

        if (HasComp<MechComponent>(target) || HasComp<BorgChassisComponent>(target))
            return false;

        if (CompOrNull<InjurableComponent>(target)?.DamageContainer == "BiologicalMetaphysical")
            return false;

        return true;
    }

    public virtual void AttachBeam(Entity<ADTMedbeamComponent> ent, EntityUid target)
    {
        ent.Comp.Target = target;
        Dirty(ent);

        var visuals = EnsureComp<ComplexJointVisualsComponent>(ent.Owner);
        visuals.Data[GetNetEntity(target)] =
            new ComplexJointVisualsData(BeamId, ent.Comp.Beam, ent.Comp.Start, ent.Comp.End, Timing.CurTime)
            {
                Scale = ent.Comp.Scale,
            };
        Dirty(ent.Owner, visuals);
    }

    public virtual void DetachBeam(Entity<ADTMedbeamComponent> ent)
    {
        if (ent.Comp.Target is not { } target)
            return;

        ent.Comp.Target = null;
        ent.Comp.Accumulator = 0;
        Dirty(ent);

        if (TryComp<ComplexJointVisualsComponent>(ent.Owner, out var visuals))
        {
            visuals.Data.Remove(GetNetEntity(target));
            if (visuals.Data.Count == 0)
                RemCompDeferred(ent.Owner, visuals);
            else
                Dirty(ent.Owner, visuals);
        }
    }

    protected EntityUid? GetHolder(Entity<ADTMedbeamComponent> ent)
    {
        if (Containers.TryGetContainingContainer((ent.Owner, null), out var container))
            return container.Owner;

        return null;
    }

    private void OnActivate(Entity<ADTMedbeamComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled)
            return;

        DetachBeam(ent);
        args.Handled = true;
    }

    private void OnHandDeselected(Entity<ADTMedbeamComponent> ent, ref HandDeselectedEvent args)
    {
        DetachBeam(ent);
    }

    private void OnDropped(Entity<ADTMedbeamComponent> ent, ref DroppedEvent args)
    {
        DetachBeam(ent);
    }

    private void OnInserted(Entity<ADTMedbeamComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        DetachBeam(ent);
    }
}