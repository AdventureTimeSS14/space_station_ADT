using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Armor.Magnetic;

public sealed class RMCMagneticSystem : EntitySystem
{
    [Dependency] private AttachableHolderSystem _attachableHolder = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMagneticItemComponent, DroppedEvent>(OnMagneticItemDropped);
    }

    private void OnMagneticItemDropped(Entity<RMCMagneticItemComponent> ent, ref DroppedEvent args)
    {
        if (_net.IsClient)
            return;

        var returnComp = EnsureComp<RMCReturnToInventoryComponent>(ent);
        returnComp.User = args.User;
        returnComp.Returned = false;
        Dirty(ent, returnComp);
    }

    public void SetMagnetizeToSlots(Entity<RMCMagneticItemComponent> ent, SlotFlags slots)
    {
        ent.Comp.MagnetizeToSlots = slots;
        Dirty(ent);
    }

    public void SetHarness(Entity<RMCMagneticItemComponent> ent, EntityUid harness, float slipChance)
    {
        ent.Comp.Harness = harness;
        ent.Comp.SlipChance = slipChance;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<RMCReturnToInventoryComponent, RMCMagneticItemComponent>();
        while (query.MoveNext(out var uid, out var comp, out var magnetic))
        {
            RemCompDeferred<RMCReturnToInventoryComponent>(uid);

            if (comp.Returned)
                continue;

            comp.Returned = true;

            var user = comp.User;
            if (TerminatingOrDeleted(user))
                continue;

            if (!TryGetFreeSlot(user, magnetic.MagnetizeToSlots, out var slotId))
                continue;

            if (TrySlip((uid, magnetic), user, slotId))
                continue;

            if (!_inventory.TryEquip(user, uid, slotId, force: true))
                continue;

            var popup = Loc.GetString("rmc-magnetize-return",
                ("item", uid),
                ("user", user));
            _popup.PopupEntity(popup, user, user, PopupType.Medium);
        }
    }

    private bool TryGetFreeSlot(EntityUid user, SlotFlags flags, out string slotId)
    {
        slotId = string.Empty;

        if (!_inventory.TryGetSlotEntity(user, "outerClothing", out _))
            return false;

        var slots = _inventory.GetSlotEnumerator(user, flags);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity != null)
                continue;

            slotId = slot.ID;
            return true;
        }

        return false;
    }

    private bool TrySlip(Entity<RMCMagneticItemComponent> ent, EntityUid user, string slotId)
    {
        if (ent.Comp.Harness is not { } harness || ent.Comp.SlipChance <= 0)
            return false;

        if (!_random.Prob(ent.Comp.SlipChance))
            return false;

        if (!TryComp<AttachableHolderComponent>(ent, out var holder))
            return false;

        if (!_attachableHolder.Detach((ent.Owner, holder), harness, user))
            return false;

        _inventory.TryEquip(user, harness, slotId, force: true);

        var popup = Loc.GetString("rmc-magnetize-slip", ("item", ent.Owner));
        _popup.PopupEntity(popup, user, user, PopupType.MediumCaution);
        return true;
    }
}
