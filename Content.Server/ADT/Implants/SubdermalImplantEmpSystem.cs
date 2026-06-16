using Content.Shared.ADT.Implants;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Implants.Components;
using Content.Server.Inventory;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Implants;

public sealed class SubdermalImplantEmpSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImplantedComponent, EmpPulseEvent>(OnMobEmped);
    }

    private void OnMobEmped(Entity<ImplantedComponent> mob, ref EmpPulseEvent args)
    {
        if (IsProtected(mob.Owner))
            return;

        if (!_container.TryGetContainer(mob.Owner, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        DamageSpecifier? damage = null;

        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (!TryComp<SubdermalImplantEmpComponent>(implant, out var empComp))
                continue;

            damage ??= empComp.EmpDamage;

            if (TryComp<SubdermalImplantComponent>(implant, out var subComp) && subComp.Action is { } action)
                _actions.SetCooldown(action, TimeSpan.FromSeconds(empComp.EmpCooldown));
        }

        if (damage != null)
        {
            _damageable.TryChangeDamage(mob.Owner, damage, ignoreResistances: true);
            Spawn("EffectSparks", Transform(mob.Owner).Coordinates);
        }

        var ev = new ImplantEmpAffectedEvent();
        RaiseLocalEvent(mob.Owner, ref ev);
    }

    private bool IsProtected(EntityUid mob)
    {
        var slots = _inventory.GetHandOrInventoryEntities(mob);
        foreach (var item in slots)
        {
            if (HasComp<ImplantEmpProtectionComponent>(item))
                return true;
        }
        return false;
    }
}
