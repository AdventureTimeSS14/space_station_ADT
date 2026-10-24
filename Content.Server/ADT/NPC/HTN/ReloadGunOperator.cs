using Content.Server.Hands.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class ReloadGunOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float SearchRange = 2.0f;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gun = GetGun(owner, blackboard);
        if (gun == null)
            return HTNOperatorStatus.Failed;

        var gunSystem = _entManager.System<SharedGunSystem>();
        var itemSlots = _entManager.System<ItemSlotsSystem>();

        var isMagazineGun = _entManager.HasComponent<MagazineAmmoProviderComponent>(gun.Value) ||
                            _entManager.HasComponent<ChamberMagazineAmmoProviderComponent>(gun.Value);

        if (isMagazineGun &&
            itemSlots.TryGetSlot(gun.Value, "gun_magazine", out var magSlot))
        {
            if (magSlot.HasItem)
            {
                var curCount = 0;
                if (magSlot.Item is { } curMag)
                {
                    var ev = new GetAmmoCountEvent();
                    _entManager.EventBus.RaiseLocalEvent(curMag, ref ev, false);
                    curCount = ev.Count;
                }

                if (curCount == 0)
                {
                    if (!itemSlots.TryEject(gun.Value, magSlot, owner, out _))
                    {
                        itemSlots.TryEject(gun.Value, magSlot, null, out _);
                    }
                }
            }

            if (!magSlot.HasItem)
            {
                var bestMag = FindBestMagazine(owner, gun.Value, magSlot, itemSlots);
                if (bestMag != null && itemSlots.TryInsert(gun.Value, magSlot, bestMag.Value, owner))
                {
                    if (_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gun.Value, out var chamberMag))
                    {
                        gunSystem.UseChambered(gun.Value, chamberMag, owner);
                    }

                    return HTNOperatorStatus.Finished;
                }
            }
        }

        if (_entManager.TryGetComponent<BallisticAmmoProviderComponent>(gun.Value, out var ballistic))
        {
            var insertedAny = false;
            while (!gunSystem.IsFull((gun.Value, ballistic)))
            {
                var candidate = FindBallisticAmmo(owner, (gun.Value, ballistic), gunSystem);
                if (candidate == null)
                    break;

                if (!gunSystem.TryBallisticInsert((gun.Value, ballistic), candidate.Value, owner))
                    break;

                insertedAny = true;
            }

            if (insertedAny)
                return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
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
                    handsSystem.TrySetActiveHand((owner, handsComp), handName);
                    return handHeldUid;
                }
            }
        }

        return null;
    }

    private EntityUid? FindBestMagazine(EntityUid owner, EntityUid gun, ItemSlot magSlot, ItemSlotsSystem itemSlots)
    {
        EntityUid? best = null;
        var bestCount = 0;

        if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            var handsSystem = _entManager.System<HandsSystem>();
            foreach (var (handName, _) in handsComp.Hands)
            {
                if (handsSystem.TryGetHeldItem(owner, handName, out var held) &&
                    held is { } heldUid &&
                    heldUid != gun)
                {
                    CheckMagCandidate(heldUid, gun, magSlot, itemSlots, ref best, ref bestCount);
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
                        CheckMagCandidate(contained, gun, magSlot, itemSlots, ref best, ref bestCount);
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

            CheckMagCandidate(nearby, gun, magSlot, itemSlots, ref best, ref bestCount);
        }

        return best;
    }

    private void CheckMagCandidate(EntityUid candidate, EntityUid gun, ItemSlot magSlot, ItemSlotsSystem itemSlots, ref EntityUid? best, ref int bestCount)
    {
        if (!itemSlots.CanInsert(gun, magSlot, candidate, null, swap: true))
            return;

        var ev = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(candidate, ref ev, false);
        if (ev.Count > bestCount)
        {
            best = candidate;
            bestCount = ev.Count;
        }
    }

    private EntityUid? FindBallisticAmmo(EntityUid owner, Entity<BallisticAmmoProviderComponent> ballistic, SharedGunSystem gunSystem)
    {
        if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            var handsSystem = _entManager.System<HandsSystem>();
            foreach (var (handName, _) in handsComp.Hands)
            {
                if (handsSystem.TryGetHeldItem(owner, handName, out var held) &&
                    held is { } heldUid &&
                    heldUid != ballistic.Owner)
                {
                    if (gunSystem.CanInsertBallistic(ballistic, heldUid))
                        return heldUid;
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
                        if (gunSystem.CanInsertBallistic(ballistic, contained))
                            return contained;
                    }
                }
            }
        }

        var xform = _entManager.GetComponent<TransformComponent>(owner);
        var lookup = _entManager.System<EntityLookupSystem>();
        foreach (var nearby in lookup.GetEntitiesInRange(xform.Coordinates, SearchRange))
        {
            if (nearby == owner || nearby == ballistic.Owner)
                continue;

            if (gunSystem.CanInsertBallistic(ballistic, nearby))
                return nearby;
        }

        return null;
    }
}
