using Content.Shared.ADT.EMP;
using Content.Shared.Power.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Emp;

public abstract partial class SharedEmpSystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    private void InitializeADT()
    {
        SubscribeLocalEvent<EmpDisablingComponent, EmpPulseEvent>(OnDisablingPulse);

        SubscribeLocalEvent<EmpContainerProtectionComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<EmpContainerProtectionComponent, EntRemovedFromContainerMessage>(OnEjected);
        SubscribeLocalEvent<EmpContainerProtectionComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<EmpContainerProtectionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnDisablingPulse(EntityUid uid, EmpDisablingComponent component, ref EmpPulseEvent args)
    {
        args.Disabled = true;
        args.Duration = component.DisablingTime;
    }

    private void OnInserted(EntityUid uid, EmpContainerProtectionComponent component, ref EntInsertedIntoContainerMessage args)
    {
        var originalResistance = 1f;

        if (TryComp<EmpResistanceComponent>(args.Entity, out var resistance))
            originalResistance = resistance.StrengthMultiplier;

        component.Batteries.TryAdd(args.Entity, originalResistance);

        EnsureComp<EmpResistanceComponent>(args.Entity).StrengthMultiplier *= component.Multiplier;
    }

    private void OnEjected(EntityUid uid, EmpContainerProtectionComponent component, ref EntRemovedFromContainerMessage args)
    {
        if (!component.Batteries.TryGetValue(args.Entity, out var original))
            return;

        component.Batteries.Remove(args.Entity);

        if (original == 1f)
            RemComp<EmpResistanceComponent>(args.Entity);
        else
            EnsureComp<EmpResistanceComponent>(args.Entity).StrengthMultiplier = original;
    }

    private void OnInit(EntityUid uid, EmpContainerProtectionComponent component, MapInitEvent args)
    {
        if (!_container.TryGetContainer(uid, component.ContainerId, out var container))
            return;

        foreach (var ent in container.ContainedEntities)
        {
            var originalResistance = 1f;

            if (TryComp<EmpResistanceComponent>(ent, out var resistance))
                originalResistance = resistance.StrengthMultiplier;

            component.Batteries.TryAdd(ent, originalResistance);

            EnsureComp<EmpResistanceComponent>(ent).StrengthMultiplier *= component.Multiplier;
        }
    }

    private void OnShutdown(EntityUid uid, EmpContainerProtectionComponent component, ComponentShutdown args)
    {
        foreach (var item in component.Batteries)
        {
            if (item.Value == 1f)
                RemComp<EmpResistanceComponent>(item.Key);
            else
                EnsureComp<EmpResistanceComponent>(item.Key).StrengthMultiplier = item.Value;
        }
    }
}
