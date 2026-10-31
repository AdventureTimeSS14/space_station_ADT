using System.Diagnostics.CodeAnalysis;
using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class CanReloadGunPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float SearchRange = 2.0f;

    [DataField("invert")]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gun = GetGun(owner, blackboard);
        if (gun == null)
            return Invert;

        var gunSystem = _entManager.System<SharedGunSystem>();
        var ammoEv = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(gun.Value, ref ammoEv);

        if (ammoEv.Count > 0)
            return Invert;

        var isMet = HasCompatibleAmmo(owner, gun.Value, gunSystem);
        return Invert ? !isMet : isMet;
    }

    private EntityUid? GetGun(EntityUid owner, NPCBlackboard blackboard)
    {
        var handsSystem = _entManager.System<HandsSystem>();
        if (blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager) &&
            handsSystem.TryGetHeldItem(owner, activeHand, out var held) &&
            held is { } heldUid &&
            _entManager.HasComponent<GunComponent>(heldUid))
        {
            return heldUid;
        }

        if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            if (handsComp.ActiveHandId != null &&
                handsSystem.TryGetHeldItem(owner, handsComp.ActiveHandId, out held) &&
                held is { } activeHeldUid &&
                _entManager.HasComponent<GunComponent>(activeHeldUid))
            {
                return activeHeldUid;
            }

            foreach (var (handName, _) in handsComp.Hands)
            {
                if (handsSystem.TryGetHeldItem(owner, handName, out var handHeld) &&
                    handHeld is { } handHeldUid &&
                    _entManager.HasComponent<GunComponent>(handHeldUid))
                {
                    return handHeldUid;
                }
            }
        }

        return null;
    }

    private bool HasCompatibleAmmo(EntityUid owner, EntityUid gun, SharedGunSystem gunSystem)
    {
        var itemSlots = _entManager.System<ItemSlotsSystem>();
        var isMagazineGun = _entManager.HasComponent<MagazineAmmoProviderComponent>(gun) ||
                            _entManager.HasComponent<ChamberMagazineAmmoProviderComponent>(gun);

        var isBallistic = _entManager.TryGetComponent<BallisticAmmoProviderComponent>(gun, out var ballistic);

        if (!isMagazineGun && !isBallistic)
            return false;

        ItemSlot? magSlot = null;
        if (isMagazineGun)
        {
            itemSlots.TryGetSlot(gun, "gun_magazine", out magSlot);
        }

        var handsSystem = _entManager.System<HandsSystem>();

        if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            foreach (var (handName, _) in handsComp.Hands)
            {
                if (handsSystem.TryGetHeldItem(owner, handName, out var held) &&
                    held is { } heldUid &&
                    heldUid != gun)
                {
                    if (IsAmmoValid(heldUid, gun, isMagazineGun, magSlot, ballistic, itemSlots, gunSystem))
                        return true;
                }
            }
        }

        if (_entManager.TryGetComponent<InventoryComponent>(owner, out var inventory))
        {
            var invSystem = _entManager.System<InventorySystem>();
            var slotEnumerator = invSystem.GetSlotEnumerator((owner, inventory));
            while (slotEnumerator.NextItem(out var item, out _))
            {
                if (_entManager.TryGetComponent<StorageComponent>(item, out var storage))
                {
                    foreach (var contained in storage.StoredItems.Keys)
                    {
                        if (IsAmmoValid(contained, gun, isMagazineGun, magSlot, ballistic, itemSlots, gunSystem))
                            return true;
                    }
                }
            }
        }

        var xform = _entManager.GetComponent<TransformComponent>(owner);
        var lookup = _entManager.System<EntityLookupSystem>();
        foreach (var nearby in lookup.GetEntitiesInRange(xform.Coordinates, SearchRange))
        {
            if (nearby == owner || nearby == gun)
                continue;

            if (IsAmmoValid(nearby, gun, isMagazineGun, magSlot, ballistic, itemSlots, gunSystem))
                return true;
        }

        return false;
    }

    private bool IsAmmoValid(EntityUid candidate, EntityUid gun, bool isMagazineGun, ItemSlot? magSlot,
        BallisticAmmoProviderComponent? ballistic, ItemSlotsSystem itemSlots, SharedGunSystem gunSystem)
    {
        if (isMagazineGun && magSlot != null)
        {
            if (itemSlots.CanInsert(gun, magSlot, candidate, null, swap: true))
            {
                var ev = new GetAmmoCountEvent();
                _entManager.EventBus.RaiseLocalEvent(candidate, ref ev, false);
                if (ev.Count > 0)
                    return true;
            }
        }

        if (ballistic != null)
        {
            if (gunSystem.CanInsertBallistic((gun, ballistic), candidate))
                return true;
        }

        return false;
    }
}
