using Content.Shared.Containers.ItemSlots;
using Content.Shared.Clothing;
using Content.Shared.Wires;
using Content.Shared.ADT.ModSuits;

namespace Content.Server.ADT.ModSuits;


public sealed class ModSuitModSystem : EntitySystem
{
    [Dependency] private readonly ModSuitSystem _modsuit = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitModComponent, ItemSlotInsertAttemptEvent>(OnInsert);
        SubscribeLocalEvent<ModSuitModComponent, ItemSlotEjectedEvent>(OnEjected);
        SubscribeLocalEvent<ModSuitModComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
    }

    private void OnInsert(EntityUid uid, ModSuitModComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (!TryComp<WiresPanelComponent>(args.SlotEntity, out var panel) || !panel.Open && args.User != null)
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
    }
    private void OnEjected(EntityUid uid, ModSuitModComponent component, ItemSlotEjectedEvent args)
    {
        if (!TryComp<ModSuitComponent>(args.SlotEntity, out var modsuit) || _modsuit.GetAttachedToggleStatus(args.SlotEntity, modsuit) != ModSuitAttachedStatus.NoneToggled)
        {
            args.Cancelled = true;
            return;
        }
        if (args.Cancelled)
            return;
        var container = modsuit.Container;
        if (container == null)
            return;

        var attachedClothings = modsuit.ClothingUids;
        if (component.Slot == "MODcore")
        {
            EntityManager.RemoveComponents(args.SlotEntity, component.Components);
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
    }
    private void OnEjectAttempt(EntityUid uid, ModSuitModComponent component, ref ItemSlotEjectAttemptEvent args)
    {
        if (!TryComp<WiresPanelComponent>(args.SlotEntity, out var panel) || !panel.Open)
        {
            args.Cancelled = true;
            return;
        }
    }
}
