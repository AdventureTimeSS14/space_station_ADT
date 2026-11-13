using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcGearSystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private ADTNpcFoundWeapon? Find(EntityUid owner, ADTNpcWeaponRole role)
    {
        var gear = GetGear(owner);

        foreach (var entry in gear.Held)
        {
            if (entry.Role == role)
                return new ADTNpcFoundWeapon(entry.Item, entry.Hand);
        }

        foreach (var entry in gear.Reserve)
        {
            if (entry.Role == role && IsCarried(owner, entry.Item))
                return new ADTNpcFoundWeapon(entry.Item, null);
        }

        return null;
    }

    public ADTNpcFoundWeapon? Resolve(EntityUid owner, ADTNpcWeaponRole role, bool strict = false)
    {
        if (role == ADTNpcWeaponRole.Main && !strict)
            return Find(owner, ADTNpcWeaponRole.Main) ?? Find(owner, ADTNpcWeaponRole.Sidearm);

        return Find(owner, role);
    }

    public bool CanSearchWeapon(EntityUid owner, ADTNpcWeaponRole? role)
    {
        if (!_gearQuery.TryComp(owner, out var gear))
            return true;

        var next = role is { } r ? gear.NextWeaponSearch.GetValueOrDefault(r) : gear.NextAnyWeaponSearch;
        return _timing.CurTime >= next;
    }

    public void DelayWeaponSearch(EntityUid owner, ADTNpcWeaponRole? role, TimeSpan delay)
    {
        if (!_gearQuery.TryComp(owner, out var gear))
            return;

        var next = _timing.CurTime + delay;

        if (role is { } r)
            gear.NextWeaponSearch[r] = next;
        else
            gear.NextAnyWeaponSearch = next;
    }

    public void GetAmmo(EntityUid gun, out int count, out int capacity)
    {
        var ev = new GetAmmoCountEvent();
        RaiseLocalEvent(gun, ref ev);
        count = ev.Count;
        capacity = ev.Capacity;
    }

    public bool IsMagazineGun(EntityUid gun, out ItemSlot? slot)
    {
        slot = null;

        if (!TryComp<ItemSlotsComponent>(gun, out var slots))
            return false;

        return _itemSlots.TryGetSlot((gun, slots), SharedGunSystem.MagazineSlot, out slot);
    }

    public bool IsLooseGun(EntityUid gun)
    {
        return !IsMagazineGun(gun, out _) && HasComp<BallisticAmmoProviderComponent>(gun);
    }

    public EntityUid? FindMagazine(EntityUid owner, EntityUid gun)
    {
        if (!IsMagazineGun(gun, out var slot) || slot == null)
            return null;

        var bestCount = 0;

        if (slot.Item is { } current)
            GetAmmo(current, out bestCount, out _);

        EntityUid? best = null;

        foreach (var entry in GetReserve(owner))
        {
            var item = entry.Item;

            if (!HasComp<BallisticAmmoProviderComponent>(item) || !IsCarried(owner, item))
                continue;

            if (!_whitelist.IsWhitelistPassOrNull(slot.Whitelist, item))
                continue;

            GetAmmo(item, out var count, out _);

            if (count <= bestCount)
                continue;

            best = item;
            bestCount = count;
        }

        return best;
    }

    private bool BoxMatches(BallisticAmmoProviderComponent box, BallisticAmmoProviderComponent gun)
    {
        var gunTags = gun.Whitelist?.Tags;

        if (gunTags == null)
            return true;

        if (box.Entities.Count > 0)
            return _whitelist.IsWhitelistPassOrNull(gun.Whitelist, box.Entities[^1]);

        var boxTags = box.Whitelist?.Tags;

        if (boxTags == null)
            return true;

        foreach (var tag in boxTags)
        {
            if (gunTags.Contains(tag))
                return true;
        }

        return false;
    }

    public (EntityUid Item, bool IsBox)? FindLooseAmmo(EntityUid owner, EntityUid gun)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(gun, out var gunProvider))
            return null;

        foreach (var entry in GetReserve(owner))
        {
            var item = entry.Item;

            if (_gunQuery.HasComp(item) || !IsCarried(owner, item))
                continue;

            if (HasComp<CartridgeAmmoComponent>(item))
            {
                if (_whitelist.IsWhitelistPassOrNull(gunProvider.Whitelist, item))
                    return (item, false);

                continue;
            }

            if (!TryComp<BallisticAmmoProviderComponent>(item, out var box))
                continue;

            GetAmmo(item, out var count, out _);

            if (count <= 0)
                continue;

            if (BoxMatches(box, gunProvider))
                return (item, true);
        }

        return null;
    }

    public bool HasReserve(EntityUid owner, EntityUid gun)
    {
        if (IsMagazineGun(gun, out _))
            return FindMagazine(owner, gun) != null;

        if (IsLooseGun(gun))
            return FindLooseAmmo(owner, gun) != null;

        return false;
    }

    public bool SwitchTo(EntityUid owner, ADTNpcWeaponRole role)
    {
        if (Resolve(owner, role) is not { } found)
            return false;

        var item = found.Item;

        if (_hands.GetActiveItem(owner) == item)
            return true;

        if (found.Hand != null)
        {
            if (role == ADTNpcWeaponRole.Main)
                StowSecondary(owner, item);

            return _hands.TrySetActiveHand(owner, found.Hand);
        }

        UnwieldHeld(owner, item);

        if (!_hands.TryGetEmptyHand(owner, out var emptyHand))
        {
            StowSecondary(owner, item);

            if (!_hands.TryGetEmptyHand(owner, out emptyHand))
                return false;
        }

        if (!_hands.TryPickup(owner, item, emptyHand, checkActionBlocker: false, animate: false))
            return false;

        return _hands.TrySetActiveHand(owner, emptyHand);
    }

    public bool SwapMagazine(EntityUid owner, EntityUid gun)
    {
        if (FindMagazine(owner, gun) is not { } fresh)
            return false;

        MarkDirty(owner);
        _itemSlots.TryEject(gun, SharedGunSystem.MagazineSlot, owner, out var old);

        if (!_itemSlots.TryInsert(gun, SharedGunSystem.MagazineSlot, fresh, owner))
        {
            if (old != null)
                _itemSlots.TryInsert(gun, SharedGunSystem.MagazineSlot, old.Value, owner);

            return false;
        }

        if (old != null)
            Stow(owner, old.Value);

        return true;
    }

    public bool LoadRound(EntityUid owner, EntityUid gun)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(gun, out var provider))
            return false;

        if (FindLooseAmmo(owner, gun) is not { } source)
            return false;

        MarkDirty(owner);
        var cartridge = source.Item;

        if (source.IsBox)
        {
            var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), Transform(owner).Coordinates, owner);
            RaiseLocalEvent(source.Item, ev);

            if (ev.Ammo.Count == 0 || ev.Ammo[0].Entity is not { } taken)
                return false;

            cartridge = taken;
        }

        if (_gun.TryBallisticInsert((gun, provider), cartridge, owner))
            return true;

        if (source.IsBox)
            Stow(owner, cartridge);

        return false;
    }
}
