using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

namespace Content.Server.ADT.NPC.Helpers;

public static class ADTNpcItemHelper
{
    private static readonly string[] ReserveSlots = { "belt", "back" };

    public static List<(string Hand, EntityUid Item)> HeldItems(IEntityManager ent, EntityUid owner)
    {
        var hands = ent.System<SharedHandsSystem>();
        var result = new List<(string, EntityUid)>();

        foreach (var hand in hands.EnumerateHands(owner).ToList())
        {
            if (hands.TryGetHeldItem(owner, hand, out var held, true))
                result.Add((hand, held.Value));
        }

        return result;
    }

    public static List<EntityUid> ReserveItems(IEntityManager ent, EntityUid owner)
    {
        var inv = ent.System<InventorySystem>();
        var result = new List<EntityUid>();

        foreach (var slot in ReserveSlots)
        {
            if (!inv.TryGetSlotEntity(owner, slot, out var container))
                continue;

            if (!ent.TryGetComponent(container.Value, out StorageComponent? storage))
                continue;

            foreach (var item in storage.Container.ContainedEntities)
            {
                result.Add(item);

                if (ent.TryGetComponent(item, out StorageComponent? inner))
                    result.AddRange(inner.Container.ContainedEntities);
            }
        }

        return result;
    }

    public static void Stow(IEntityManager ent, EntityUid owner, EntityUid item)
    {
        var hands = ent.System<SharedHandsSystem>();
        var storage = ent.System<SharedStorageSystem>();
        var inv = ent.System<InventorySystem>();

        foreach (var (_, held) in HeldItems(ent, owner))
        {
            if (held != item)
                continue;

            hands.TryDrop(owner, item, checkActionBlocker: false, doDropInteraction: false);
            break;
        }

        foreach (var slot in ReserveSlots)
        {
            if (!inv.TryGetSlotEntity(owner, slot, out var container))
                continue;

            if (!ent.HasComponent<StorageComponent>(container.Value))
                continue;

            if (storage.Insert(container.Value, item, out _, user: owner, playSound: false))
                return;
        }
    }

    public static void StowSecondary(IEntityManager ent, EntityUid owner, EntityUid keep)
    {
        foreach (var (_, item) in HeldItems(ent, owner))
        {
            if (item == keep)
                continue;

            var role = ADTNpcWeaponHelper.Classify(ent, item);

            if (role is ADTNpcWeaponRole.Sidearm or ADTNpcWeaponRole.Melee)
                Stow(ent, owner, item);
        }
    }

    public static bool MakeRoom(IEntityManager ent, EntityUid owner, out string? hand)
    {
        var hands = ent.System<SharedHandsSystem>();

        if (hands.TryGetEmptyHand(owner, out hand))
            return true;

        UnwieldHeld(ent, owner, EntityUid.Invalid);

        if (hands.TryGetEmptyHand(owner, out hand))
            return true;

        StowSecondary(ent, owner, EntityUid.Invalid);
        return hands.TryGetEmptyHand(owner, out hand);
    }

    public static void UnwieldHeld(IEntityManager ent, EntityUid owner, EntityUid except)
    {
        var wield = ent.System<SharedWieldableSystem>();

        foreach (var (_, item) in HeldItems(ent, owner))
        {
            if (item == except)
                continue;

            if (ent.TryGetComponent(item, out WieldableComponent? wieldable) && wieldable.Wielded)
                wield.TryUnwield((item, wieldable), owner, force: true);
        }
    }
}
