using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.Helpers;

public static class ADTNpcWeaponHelper
{
    public static readonly ProtoId<TagPrototype> SidearmTag = "Sidearm";

    public static readonly ProtoId<TagPrototype>[] MeleeTags = { "Knife" };

    public readonly record struct Found(EntityUid Item, string? Hand);

    public static ADTNpcWeaponRole? Classify(IEntityManager ent, EntityUid item)
    {
        var tags = ent.System<TagSystem>();

        if (tags.HasAnyTag(item, MeleeTags))
            return ADTNpcWeaponRole.Melee;

        if (!ent.HasComponent<GunComponent>(item))
            return null;

        return tags.HasTag(item, SidearmTag) ? ADTNpcWeaponRole.Sidearm : ADTNpcWeaponRole.Main;
    }

    private static Found? Find(IEntityManager ent, EntityUid owner, ADTNpcWeaponRole role)
    {
        foreach (var (hand, item) in ADTNpcItemHelper.HeldItems(ent, owner))
        {
            if (Classify(ent, item) == role)
                return new Found(item, hand);
        }

        foreach (var item in ADTNpcItemHelper.ReserveItems(ent, owner))
        {
            if (Classify(ent, item) == role)
                return new Found(item, null);
        }

        return null;
    }

    public static Found? Resolve(IEntityManager ent, EntityUid owner, ADTNpcWeaponRole role, bool strict = false)
    {
        if (role == ADTNpcWeaponRole.Main && !strict)
            return Find(ent, owner, ADTNpcWeaponRole.Main) ?? Find(ent, owner, ADTNpcWeaponRole.Sidearm);

        return Find(ent, owner, role);
    }

    public static void GetAmmo(IEntityManager ent, EntityUid gun, out int count, out int capacity)
    {
        var ev = new GetAmmoCountEvent();
        ent.EventBus.RaiseLocalEvent(gun, ref ev);
        count = ev.Count;
        capacity = ev.Capacity;
    }

    public static bool IsMagazineGun(IEntityManager ent, EntityUid gun, out ItemSlot? slot)
    {
        slot = null;

        if (!ent.TryGetComponent(gun, out ItemSlotsComponent? slots))
            return false;

        return ent.System<ItemSlotsSystem>().TryGetSlot((gun, slots), SharedGunSystem.MagazineSlot, out slot);
    }

    public static bool IsLooseGun(IEntityManager ent, EntityUid gun)
    {
        return !IsMagazineGun(ent, gun, out _) && ent.HasComponent<BallisticAmmoProviderComponent>(gun);
    }

    public static EntityUid? FindMagazine(IEntityManager ent, EntityUid owner, EntityUid gun)
    {
        if (!IsMagazineGun(ent, gun, out var slot) || slot == null)
            return null;

        var whitelist = ent.System<EntityWhitelistSystem>();
        var bestCount = 0;

        if (slot.Item is { } current)
        {
            GetAmmo(ent, current, out var currentCount, out _);
            bestCount = currentCount;
        }

        EntityUid? best = null;

        foreach (var item in ADTNpcItemHelper.ReserveItems(ent, owner))
        {
            if (!ent.HasComponent<BallisticAmmoProviderComponent>(item))
                continue;

            if (!whitelist.IsWhitelistPassOrNull(slot.Whitelist, item))
                continue;

            GetAmmo(ent, item, out var count, out _);

            if (count <= bestCount)
                continue;

            best = item;
            bestCount = count;
        }

        return best;
    }

    private static bool BoxMatches(IEntityManager ent, BallisticAmmoProviderComponent box, BallisticAmmoProviderComponent gun)
    {
        var gunTags = gun.Whitelist?.Tags;

        if (gunTags == null)
            return true;

        if (box.Entities.Count > 0)
            return ent.System<EntityWhitelistSystem>().IsWhitelistPassOrNull(gun.Whitelist, box.Entities[^1]);

        var boxTags = box.Whitelist?.Tags;

        if (boxTags == null)
            return true;

        return boxTags.Any(tag => gunTags.Contains(tag));
    }

    public static (EntityUid Item, bool IsBox)? FindLooseAmmo(IEntityManager ent, EntityUid owner, EntityUid gun)
    {
        if (!ent.TryGetComponent(gun, out BallisticAmmoProviderComponent? gunProvider))
            return null;

        var whitelist = ent.System<EntityWhitelistSystem>();

        foreach (var item in ADTNpcItemHelper.ReserveItems(ent, owner))
        {
            if (ent.HasComponent<GunComponent>(item))
                continue;

            if (ent.HasComponent<CartridgeAmmoComponent>(item))
            {
                if (whitelist.IsWhitelistPassOrNull(gunProvider.Whitelist, item))
                    return (item, false);

                continue;
            }

            if (!ent.TryGetComponent(item, out BallisticAmmoProviderComponent? box))
                continue;

            GetAmmo(ent, item, out var count, out _);

            if (count <= 0)
                continue;

            if (BoxMatches(ent, box, gunProvider))
                return (item, true);
        }

        return null;
    }

    public static bool HasReserve(IEntityManager ent, EntityUid owner, EntityUid gun)
    {
        if (IsMagazineGun(ent, gun, out _))
            return FindMagazine(ent, owner, gun) != null;

        if (IsLooseGun(ent, gun))
            return FindLooseAmmo(ent, owner, gun) != null;

        return false;
    }

    public static bool SwitchTo(IEntityManager ent, EntityUid owner, ADTNpcWeaponRole role)
    {
        var hands = ent.System<SharedHandsSystem>();

        if (Resolve(ent, owner, role) is not { } found)
            return false;

        var item = found.Item;

        if (hands.GetActiveItem(owner) == item)
            return true;

        if (found.Hand != null)
        {
            if (role == ADTNpcWeaponRole.Main)
                ADTNpcItemHelper.StowSecondary(ent, owner, item);

            return hands.TrySetActiveHand(owner, found.Hand);
        }

        ADTNpcItemHelper.UnwieldHeld(ent, owner, item);

        if (!hands.TryGetEmptyHand(owner, out var emptyHand))
        {
            ADTNpcItemHelper.StowSecondary(ent, owner, item);

            if (!hands.TryGetEmptyHand(owner, out emptyHand))
                return false;
        }

        if (!hands.TryPickup(owner, item, emptyHand, checkActionBlocker: false, animate: false))
            return false;

        return hands.TrySetActiveHand(owner, emptyHand);
    }

    public static bool SwapMagazine(IEntityManager ent, EntityUid owner, EntityUid gun)
    {
        if (FindMagazine(ent, owner, gun) is not { } fresh)
            return false;

        var slots = ent.System<ItemSlotsSystem>();

        slots.TryEject(gun, SharedGunSystem.MagazineSlot, owner, out var old);

        if (!slots.TryInsert(gun, SharedGunSystem.MagazineSlot, fresh, owner))
        {
            if (old != null)
                slots.TryInsert(gun, SharedGunSystem.MagazineSlot, old.Value, owner);

            return false;
        }

        if (old != null)
            ADTNpcItemHelper.Stow(ent, owner, old.Value);

        return true;
    }

    public static bool LoadRound(IEntityManager ent, EntityUid owner, EntityUid gun)
    {
        if (!ent.TryGetComponent(gun, out BallisticAmmoProviderComponent? provider))
            return false;

        if (FindLooseAmmo(ent, owner, gun) is not { } source)
            return false;

        var gunSystem = ent.System<SharedGunSystem>();
        var cartridge = source.Item;

        if (source.IsBox)
        {
            var coords = ent.GetComponent<TransformComponent>(owner).Coordinates;
            var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), coords, owner);
            ent.EventBus.RaiseLocalEvent(source.Item, ev);

            if (ev.Ammo.Count == 0 || ev.Ammo[0].Entity is not { } taken)
                return false;

            cartridge = taken;
        }

        if (gunSystem.TryBallisticInsert((gun, provider), cartridge, owner))
            return true;

        if (source.IsBox)
            ADTNpcItemHelper.Stow(ent, owner, cartridge);

        return false;
    }
}
