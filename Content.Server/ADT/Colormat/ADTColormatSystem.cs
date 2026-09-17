using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Colormat;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Colormat;

public sealed class ADTColormatSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTColormatComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ADTColormatComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ADTColormatComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ADTColormatComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

        Subs.BuiEvents<ADTColormatComponent>(ADTColormatUiKey.Key, subs =>
        {
            subs.Event<ADTColormatSetColorMessage>(OnSetColor);
            subs.Event<ADTColormatEjectMessage>(OnEject);
        });
    }

    private void OnInsertAttempt(EntityUid uid, ADTColormatComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID != component.SlotId)
            return;

        if (!this.IsPowered(uid, EntityManager))
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<ToolComponent>(args.Item))
        {
            args.Cancelled = true;
            if (args.User is { } user)
                _popup.PopupEntity(Loc.GetString("colormat-tool-denied"), uid, user);
        }
    }

    private void OnInserted(EntityUid uid, ADTColormatComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.SlotId)
            return;

        UpdateUiState(uid, component);
    }

    private void OnRemoved(EntityUid uid, ADTColormatComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.SlotId)
            return;

        UpdateUiState(uid, component);
    }

    private void OnBeforeUiOpen(Entity<ADTColormatComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUiState(ent.Owner, ent.Comp);
    }

    private void OnSetColor(EntityUid uid, ADTColormatComponent component, ADTColormatSetColorMessage msg)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        var item = _itemSlots.GetItemOrNull(uid, component.SlotId);
        if (item is not { } target)
            return;

        var paint = EnsureComp<ADTClothingPaintComponent>(target);
        paint.PaintColor = msg.Color;
        Dirty(target, paint);
    }

    private void OnEject(EntityUid uid, ADTColormatComponent component, ADTColormatEjectMessage msg)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (msg.Actor is not { Valid: true } actor)
            return;

        if (!_itemSlots.TryGetSlot(uid, component.SlotId, out var slot))
            return;

        if (_itemSlots.TryEjectToHands(uid, slot, actor))
            UpdateUiState(uid, component);
    }

    private void UpdateUiState(EntityUid uid, ADTColormatComponent component)
    {
        var item = _itemSlots.GetItemOrNull(uid, component.SlotId);
        _ui.SetUiState(uid, ADTColormatUiKey.Key, new ADTColormatUiState(
            item is { } value ? GetNetEntity(value) : null));
    }
}
