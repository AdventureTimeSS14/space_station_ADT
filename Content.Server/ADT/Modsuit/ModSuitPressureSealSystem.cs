using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.ModSuits;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Modsuit;

public sealed class ModSuitPressureSealSystem : EntitySystem
{
    [Dependency] private readonly BarotraumaSystem _barotrauma = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModPartComponent, GetPressureProtectionValuesEvent>(OnGetPressureProtection);
        SubscribeLocalEvent<ModPartComponent, GotEquippedEvent>(OnPartEquipped);
        SubscribeLocalEvent<ModPartComponent, EntGotInsertedIntoContainerMessage>(OnPartReturned);
    }

    private void OnGetPressureProtection(Entity<ModPartComponent> ent, ref GetPressureProtectionValuesEvent args)
    {
        if (IsSuitSealed(ent))
            return;

        args.HighPressureMultiplier = 1f;
        args.HighPressureModifier = 0f;
        args.LowPressureMultiplier = 1f;
        args.LowPressureModifier = 0f;
    }

    private void OnPartEquipped(Entity<ModPartComponent> ent, ref GotEquippedEvent args)
    {
        _barotrauma.RefreshPressureResistance(args.EquipTarget);
    }

    private void OnPartReturned(Entity<ModPartComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ModSuitComponent.DefaultClothingContainerId)
            return;

        if (TryComp<ModSuitComponent>(args.Container.Owner, out var modSuit)
            && modSuit.TempUser is { } wearer)
        {
            _barotrauma.RefreshPressureResistance(wearer);
        }
    }

    private bool IsSuitSealed(Entity<ModPartComponent> ent)
    {
        var suit = GetEntity(ent.Comp.AttachedUid);

        if (!TryComp<ModSuitComponent>(suit, out var modSuit) || modSuit.TempUser is not { } wearer)
            return true;

        foreach (var (netPart, slot) in modSuit.ClothingUids)
        {
            if (!_inventory.TryGetSlotEntity(wearer, slot, out var equipped)
                || equipped != GetEntity(netPart))
            {
                return false;
            }
        }

        return true;
    }
}
