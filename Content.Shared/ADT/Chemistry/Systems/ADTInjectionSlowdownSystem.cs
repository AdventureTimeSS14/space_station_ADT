using Content.Shared.ADT.Chemistry.Components;
using Content.Shared.Inventory;

namespace Content.Shared.ADT.Chemistry.Systems;

public sealed class ADTInjectionSlowdownSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public bool IsSuited(EntityUid target)
    {
        if (!_inventory.TryGetSlotEntity(target, "outerClothing", out var suit)
            || !TryComp<ADTInjectionSlowdownComponent>(suit, out var slowdown))
        {
            return false;
        }

        if (!slowdown.RequiresHelmet)
            return true;

        return _inventory.TryGetSlotEntity(target, "head", out var helmet)
            && HasComp<ADTInjectionSlowdownComponent>(helmet);
    }
}
