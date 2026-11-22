using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcGearSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedWieldableSystem _wield = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly string[] ReserveSlots = { "belt", "back" };
    private static readonly ProtoId<TagPrototype> SidearmTag = "Sidearm";
    private static readonly ProtoId<TagPrototype>[] MeleeTags = { "Knife" };

    private EntityQuery<ADTNpcGearComponent> _gearQuery;
    private EntityQuery<StorageComponent> _storageQuery;
    private EntityQuery<GunComponent> _gunQuery;

    public override void Initialize()
    {
        base.Initialize();

        _gearQuery = GetEntityQuery<ADTNpcGearComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();
        _gunQuery = GetEntityQuery<GunComponent>();

        SubscribeLocalEvent<ADTNpcGearComponent, DidEquipHandEvent>(OnGearChanged);
        SubscribeLocalEvent<ADTNpcGearComponent, DidUnequipHandEvent>(OnGearChanged);
        SubscribeLocalEvent<ADTNpcGearComponent, DidEquipEvent>(OnGearChanged);
        SubscribeLocalEvent<ADTNpcGearComponent, DidUnequipEvent>(OnGearChanged);
    }

    private void OnGearChanged<T>(EntityUid uid, ADTNpcGearComponent component, T args)
    {
        component.Dirty = true;
    }

    public void MarkDirty(EntityUid owner)
    {
        if (_gearQuery.TryComp(owner, out var gear))
            gear.Dirty = true;
    }

    private ADTNpcGearComponent GetGear(EntityUid owner)
    {
        if (!_gearQuery.TryComp(owner, out var gear))
        {
            gear = new ADTNpcGearComponent();
            Rebuild(owner, gear);
            return gear;
        }

        if (gear.Dirty || _timing.CurTime >= gear.NextRefresh)
            Rebuild(owner, gear);

        return gear;
    }

    private void Rebuild(EntityUid owner, ADTNpcGearComponent gear)
    {
        gear.Held.Clear();
        gear.Reserve.Clear();

        foreach (var hand in _hands.EnumerateHands(owner))
        {
            if (_hands.TryGetHeldItem(owner, hand, out var held, true))
                gear.Held.Add(new ADTNpcHeldItem(hand, held.Value, Classify(held.Value)));
        }

        foreach (var slot in ReserveSlots)
        {
            if (!_inventory.TryGetSlotEntity(owner, slot, out var container))
                continue;

            if (!_storageQuery.TryComp(container.Value, out var storage))
                continue;

            foreach (var item in storage.Container.ContainedEntities)
            {
                gear.Reserve.Add(new ADTNpcReserveItem(item, Classify(item)));

                if (!_storageQuery.TryComp(item, out var inner))
                    continue;

                foreach (var innerItem in inner.Container.ContainedEntities)
                {
                    gear.Reserve.Add(new ADTNpcReserveItem(innerItem, Classify(innerItem)));
                }
            }
        }

        gear.Dirty = false;
        gear.NextRefresh = _timing.CurTime + gear.RefreshInterval;
    }

    public IReadOnlyList<ADTNpcHeldItem> GetHeld(EntityUid owner)
    {
        return GetGear(owner).Held;
    }

    public IReadOnlyList<ADTNpcReserveItem> GetReserve(EntityUid owner)
    {
        return GetGear(owner).Reserve;
    }

    public bool IsCarried(EntityUid owner, EntityUid item)
    {
        if (TerminatingOrDeleted(item))
            return false;

        var parent = Transform(item).ParentUid;

        for (var i = 0; i < 3 && parent.IsValid(); i++)
        {
            if (parent == owner)
                return true;

            parent = Transform(parent).ParentUid;
        }

        return false;
    }

    public ADTNpcWeaponRole? Classify(EntityUid item)
    {
        if (_tag.HasAnyTag(item, MeleeTags))
            return ADTNpcWeaponRole.Melee;

        if (!_gunQuery.HasComp(item))
            return null;

        return _tag.HasTag(item, SidearmTag) ? ADTNpcWeaponRole.Sidearm : ADTNpcWeaponRole.Main;
    }

    public EntityUid? FindFood(EntityUid owner)
    {
        foreach (var entry in GetReserve(owner))
        {
            if (HasComp<EdibleComponent>(entry.Item) && IsCarried(owner, entry.Item))
                return entry.Item;
        }

        return null;
    }

    public void Stow(EntityUid owner, EntityUid item)
    {
        if (_hands.IsHolding(owner, item))
            _hands.TryDrop(owner, item, checkActionBlocker: false, doDropInteraction: false);

        MarkDirty(owner);

        foreach (var slot in ReserveSlots)
        {
            if (!_inventory.TryGetSlotEntity(owner, slot, out var container))
                continue;

            if (!_storageQuery.HasComp(container.Value))
                continue;

            if (_storage.Insert(container.Value, item, out _, user: owner, playSound: false))
                return;
        }
    }

    public void StowSecondary(EntityUid owner, EntityUid keep)
    {
        var held = GetHeld(owner);

        for (var i = held.Count - 1; i >= 0; i--)
        {
            var entry = held[i];

            if (entry.Item == keep)
                continue;

            if (entry.Role is ADTNpcWeaponRole.Sidearm or ADTNpcWeaponRole.Melee)
                Stow(owner, entry.Item);
        }
    }

    public bool MakeRoom(EntityUid owner, out string? hand)
    {
        if (_hands.TryGetEmptyHand(owner, out hand))
            return true;

        UnwieldHeld(owner, EntityUid.Invalid);

        if (_hands.TryGetEmptyHand(owner, out hand))
            return true;

        StowSecondary(owner, EntityUid.Invalid);
        return _hands.TryGetEmptyHand(owner, out hand);
    }

    public void UnwieldHeld(EntityUid owner, EntityUid except)
    {
        foreach (var entry in GetHeld(owner))
        {
            if (entry.Item == except)
                continue;

            if (TryComp<WieldableComponent>(entry.Item, out var wieldable) && wieldable.Wielded)
                _wield.TryUnwield((entry.Item, wieldable), owner, force: true);
        }
    }

    public bool TakeInHand(EntityUid owner, EntityUid item, out string? hand)
    {
        if (!MakeRoom(owner, out hand) || hand == null)
            return false;

        if (!_hands.TryPickup(owner, item, hand, checkActionBlocker: false, animate: false))
            return false;

        _hands.TrySetActiveHand(owner, hand);
        return true;
    }
}
