using Content.Shared.ADT.EMP;
using Content.Shared.Power.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Emp;

public abstract partial class SharedEmpSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private void InitializeADT()
    {
        SubscribeLocalEvent<EmpDisablingComponent, EmpPulseEvent>(OnDisablingPulse);
        SubscribeLocalEvent<EmpProtectionComponent, EmpAttemptEvent>(OnProtectionEmp);

        SubscribeLocalEvent<EmpContainerProtectionComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<EmpContainerProtectionComponent, EntRemovedFromContainerMessage>(OnEjected);
        SubscribeLocalEvent<EmpContainerProtectionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmpContainerProtectionComponent, MapInitEvent>(OnInit);
    }

    private void OnDisablingPulse(EntityUid uid, EmpDisablingComponent component, ref EmpPulseEvent args)
    {
        args.Disabled = true;
        args.Duration = component.DisablingTime;
    }

    private void OnProtectionEmp(EntityUid uid, EmpProtectionComponent component, ref EmpAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInserted(EntityUid uid, EmpContainerProtectionComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (HasComp<EmpProtectionComponent>(args.Entity))
            return;

        EnsureComp<EmpProtectionComponent>(args.Entity);
        component.Batteries.Add(args.Entity);
    }

    private void OnEjected(EntityUid uid, EmpContainerProtectionComponent component, ref EntRemovedFromContainerMessage args)
    {
        if (!component.Batteries.Contains(args.Entity))
            return;

        RemComp<EmpProtectionComponent>(args.Entity);
        component.Batteries.Remove(args.Entity);
    }

    private void OnShutdown(EntityUid uid, EmpContainerProtectionComponent component, ComponentShutdown args)
    {
        foreach (var item in component.Batteries)
            RemComp<EmpProtectionComponent>(item);
    }

    private void OnInit(EntityUid uid, EmpContainerProtectionComponent component, MapInitEvent args)
    {
        var containers = _container.GetAllContainers(uid);
        foreach (var container in containers)
        {
            foreach (var item in container.ContainedEntities)
            {
                if (!HasComp<BatteryComponent>(item) || HasComp<EmpProtectionComponent>(item))
                    continue;

                EnsureComp<EmpProtectionComponent>(item);
                component.Batteries.Add(item);
            }
        }
    }
}
