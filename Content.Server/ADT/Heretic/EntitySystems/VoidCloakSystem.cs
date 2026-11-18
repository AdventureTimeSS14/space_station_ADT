using Content.Server.Atmos.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Server.Heretic.EntitySystems;

public sealed class VoidCloakSystem : SharedVoidCloakSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, GetPressureProtectionValuesEvent>(OnGetPressureProtectionValues);
        SubscribeLocalEvent<VoidCloakComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<VoidCloakComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnGetPressureProtectionValues(Entity<InventoryComponent> ent, ref GetPressureProtectionValuesEvent args)
    {
        if (!_inventory.TryGetSlotEntity(ent, "outerClothing", out var entity, ent.Comp))
            return;

        if (!TryComp(entity, out VoidCloakComponent? cloak) || cloak.Transparent)
            return;

        args.LowPressureMultiplier *= 1000f;
    }

    private void OnEquipped(Entity<VoidCloakComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Slot != "outerClothing" || ent.Comp.Transparent)
            return;

        EnsureComp<PressureProtectionComponent>(args.EquipTarget);
    }

    private void OnUnequipped(Entity<VoidCloakComponent> ent, ref GotUnequippedEvent args)
    {
        RemCompDeferred<PressureProtectionComponent>(args.EquipTarget);
    }

    protected override void UpdatePressureProtection(EntityUid cloak, bool enabled)
    {
        base.UpdatePressureProtection(cloak, enabled);

        if (TerminatingOrDeleted(cloak))
            return;

        if (!_container.TryGetContainingContainer(cloak, out var container) || container.ID != "outerClothing")
            return;

        if (enabled)
            EnsureComp<PressureProtectionComponent>(container.Owner);
        else
            RemCompDeferred<PressureProtectionComponent>(container.Owner);
    }
}