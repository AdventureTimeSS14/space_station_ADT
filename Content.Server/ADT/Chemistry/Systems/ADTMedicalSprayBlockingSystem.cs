using Content.Server.Atmos.Components;
using Content.Shared.ADT.Chemistry.Events;
using Content.Shared.Inventory;

namespace Content.Server.ADT.Chemistry.Systems;

public sealed class ADTMedicalSprayBlockingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExposedSkinAttemptEvent>(OnExposedSkinAttempt);
    }

    private void OnExposedSkinAttempt(ref ExposedSkinAttemptEvent args)
    {
        if (HasFullPressureProtection(args.Target))
            args.Cancelled = true;
    }

    private bool HasFullPressureProtection(EntityUid target)
    {
        if (!TryComp<BarotraumaComponent>(target, out var barotrauma) || barotrauma.ProtectionSlots.Count == 0)
            return HasAnyPressureProtection(target);

        foreach (var slot in barotrauma.ProtectionSlots)
        {
            if (!_inventory.TryGetSlotEntity(target, slot, out var equipment)
                || !HasComp<PressureProtectionComponent>(equipment.Value))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAnyPressureProtection(EntityUid target)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(target, out var enumerator, SlotFlags.WITHOUT_POCKET))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (HasComp<PressureProtectionComponent>(item))
                return true;
        }

        return false;
    }
}