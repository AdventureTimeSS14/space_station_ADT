using Content.Shared.ADT.CantShoot;
using Content.Shared.ADT.EMP;
using Content.Shared.Power.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Emp;

public abstract partial class SharedEmpSystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    private void InitializeADT()
    {
        SubscribeLocalEvent<EmpDisablingComponent, EmpPulseEvent>(OnDisablingPulse);
        SubscribeLocalEvent<EmpDisabledComponent, ShotAttemptedEvent>(OnDisabledShot);

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

    private void OnDisabledShot(EntityUid uid, EmpDisabledComponent component, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void OnInserted(Entity<EmpContainerProtectionComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        StartProtection(ent, args.Entity);
    }

    private void OnEjected(Entity<EmpContainerProtectionComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        StopProtection(ent, args.Entity);
    }

    private void StartProtection(Entity<EmpContainerProtectionComponent> ent, EntityUid battery)
    {
        if (!HasComp<BatteryComponent>(battery))
            return;

        var originalResistance = 1f;
        bool cap = TryComp<EmpPowerDrainCapComponent>(battery, out var drainCap);
        bool capRelToMax = drainCap != null && drainCap.RelativeToMaxCharge;
        float capPercentage = drainCap != null ? drainCap.CapPercentage : 1;

        if (TryComp<EmpResistanceComponent>(battery, out var resistance))
            originalResistance = resistance.StrengthMultiplier;

        if (ent.Comp.CapEnergyConsumption)
        {
            drainCap = EnsureComp<EmpPowerDrainCapComponent>(battery);
            drainCap.RelativeToMaxCharge = ent.Comp.CapRelativeToMaxCharge;
            drainCap.CapPercentage = ent.Comp.CapPercentage;
        }

        var baseBatteryData = new EmpContainerProtectionComponent.BatteryData(originalResistance, cap, capRelToMax, capPercentage);
        ent.Comp.Batteries.TryAdd(battery, baseBatteryData);

        EnsureComp<EmpResistanceComponent>(battery).StrengthMultiplier *= ent.Comp.Multiplier;
    }

    private void StopProtection(Entity<EmpContainerProtectionComponent> ent, EntityUid battery)
    {
        if (!ent.Comp.Batteries.TryGetValue(battery, out var original))
            return;

        if (original.Protection == 1f)
            RemComp<EmpResistanceComponent>(battery);
        else
            EnsureComp<EmpResistanceComponent>(battery).StrengthMultiplier = original.Protection;

        if (!original.CapConsumption)
            RemComp<EmpPowerDrainCapComponent>(battery);
        else
        {
            var cap = EnsureComp<EmpPowerDrainCapComponent>(battery);
            cap.RelativeToMaxCharge = original.CapRelativeToMax;
            cap.CapPercentage = original.CapPercentage;
        }

        ent.Comp.Batteries.Remove(battery);
    }

    private void OnInit(Entity<EmpContainerProtectionComponent> ent, ref MapInitEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        foreach (var item in container.ContainedEntities)
        {
            StartProtection(ent, item);
        }
    }

    private void OnShutdown(Entity<EmpContainerProtectionComponent> ent, ref ComponentShutdown args)
    {
        foreach (var item in ent.Comp.Batteries)
        {
            StopProtection(ent, item.Key);
        }
    }
}
