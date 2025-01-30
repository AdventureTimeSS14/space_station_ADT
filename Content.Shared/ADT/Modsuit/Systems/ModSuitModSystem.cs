using Content.Shared.Containers.ItemSlots;
using Content.Shared.Clothing;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.ModSuits;


public sealed class ModSuitModSystem : EntitySystem
{
    [Dependency] private readonly ClothingSpeedModifierSystem _clothing = default!;
    [Dependency] private readonly ModSuitSystem _modsuit = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitModComponent, ItemSlotInsertAttemptEvent>(OnInsert);
        SubscribeLocalEvent<ModSuitModComponent, ItemSlotEjectAttemptEvent>(OnEject);
    }

    private void OnInsert(EntityUid uid, ModSuitModComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled || component.Inserted)
            return;
        if (!TryComp<WiresPanelComponent>(args.SlotEntity, out var panel) || !panel.Open)
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp<ModSuitComponent>(args.SlotEntity, out var modsuit) || _modsuit.GetAttachedToggleStatus(args.SlotEntity, modsuit) != ModSuitAttachedStatus.NoneToggled)
        {
            args.Cancelled = true;
            return;
        }

        var container = modsuit.Container;
        if (container == null)
            return;
        var attachedClothings = modsuit.ClothingUids;
        if (component.Slot == "MODcore")
        {
            EntityManager.AddComponents(args.SlotEntity, component.Components);
            component.Inserted = true;
            return;
        }

        foreach (var attached in attachedClothings)
        {
            if (!container.Contains(attached.Key))
                continue;
            if (attached.Value != component.Slot)
                continue;
            EntityManager.AddComponents(attached.Key, component.Components);
            if (component.RemoveComponents != null)
                EntityManager.RemoveComponents(attached.Key, component.RemoveComponents);
            break;
        }

        if (TryComp<ClothingSpeedModifierComponent>(args.SlotEntity, out var modify))
        {
            _clothing.ModifySpeed(uid, modify, component.SpeedMod);
            component.Inserted = true;
        }
    }
    private void OnEject(EntityUid uid, ModSuitModComponent component, ref ItemSlotEjectAttemptEvent args)
    {
        if (!TryComp<WiresPanelComponent>(args.SlotEntity, out var panel) || !panel.Open)
        {
            args.Cancelled = true;
            return;
        }
        if (args.Cancelled || !component.Inserted)
            return;
        if (!TryComp<ModSuitComponent>(args.SlotEntity, out var modsuit))
            return;
        var container = modsuit.Container;
        if (container == null)
            return;
        var attachedClothings = modsuit.ClothingUids;
        if (component.Slot == "MODcore")
        {
            EntityManager.AddComponents(args.SlotEntity, component.Components);
            component.Inserted = false;
            return;
        }

        foreach (var attached in attachedClothings)
        {
            if (!container.Contains(attached.Key))
                continue;
            if (attached.Value != component.Slot)
                continue;
            EntityManager.RemoveComponents(attached.Key, component.Components);
            if (component.RemoveComponents != null)
                EntityManager.AddComponents(attached.Key, component.RemoveComponents);
            break;
        }

        if (TryComp<ClothingSpeedModifierComponent>(args.SlotEntity, out var modify))
        {
            _clothing.ModifySpeed(uid, modify, -component.SpeedMod);
            component.Inserted = false;
        }
    }
}
