using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Shared.SubFloor;

public abstract partial class SharedTrayScannerSystem
{
    [Dependency] private SharedContainerSystem _containers = default!;

    [SubscribeLocalEvent]
    private void OnTrayStartup(Entity<TrayScannerComponent> ent, ref ComponentStartup args)
    {
        if (TryGetWearer(ent, out var user))
            OnEquip(user);
    }

    [SubscribeLocalEvent]
    private void OnTrayShutdown(Entity<TrayScannerComponent> ent, ref ComponentShutdown args)
    {
        if (TryGetWearer(ent, out var user))
            OnUnequip(user);
    }

    private bool TryGetWearer(EntityUid uid, out EntityUid user)
    {
        user = default;

        if (!_containers.TryGetContainingContainer((uid, null), out var container)
            || !HasComp<InventoryComponent>(container.Owner))
        {
            return false;
        }

        user = container.Owner;
        return true;
    }
}
