using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.Preconditions;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.Humanoid.Systems;
using Content.Shared.ADT.NPC;
using Content.Shared.ADT.TTS;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Events;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Preferences;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN;

public enum ADTNpcWeaponRole
{
    Main,
    Sidearm,
    Melee,
}

public enum ADTNpcWeaponCheck
{
    Exists,
    Held,
    Active,
    HasAmmo,
    Empty,
    NotFull,
    CanReload,
    Usable,
    Magazine,
    Loose,
    ActiveNonWeapon,
}

public static class ADTNpcWeaponHelper
{
    public static readonly ProtoId<TagPrototype> SidearmTag = "Sidearm";

    public static readonly ProtoId<TagPrototype>[] MeleeTags = { "Knife" };

    private static readonly string[] ReserveSlots = { "belt", "back" };

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

    private static Found? Find(IEntityManager ent, EntityUid owner, ADTNpcWeaponRole role)
    {
        foreach (var (hand, item) in HeldItems(ent, owner))
        {
            if (Classify(ent, item) == role)
                return new Found(item, hand);
        }

        foreach (var item in ReserveItems(ent, owner))
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

        foreach (var item in ReserveItems(ent, owner))
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

        foreach (var item in ReserveItems(ent, owner))
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

    private static void StowSecondary(IEntityManager ent, EntityUid owner, EntityUid keep)
    {
        foreach (var (_, item) in HeldItems(ent, owner))
        {
            if (item == keep)
                continue;

            var role = Classify(ent, item);

            if (role is ADTNpcWeaponRole.Sidearm or ADTNpcWeaponRole.Melee)
                Stow(ent, owner, item);
        }
    }

    public static float DamageFraction(IEntityManager ent, EntityUid uid)
    {
        var thresholds = ent.System<MobThresholdSystem>();

        if (!thresholds.TryGetThresholdForState(uid, MobState.SoftCritical, out var amount) &&
            !thresholds.TryGetThresholdForState(uid, MobState.Critical, out amount))
        {
            return 0f;
        }

        var total = ent.System<DamageableSystem>().GetTotalDamage(uid);
        return (float) (total / amount.Value);
    }

    public static bool IsBleeding(IEntityManager ent, EntityUid uid)
    {
        return ent.TryGetComponent(uid, out BloodstreamComponent? bloodstream) && bloodstream.BleedAmount > 0f;
    }

    public static int HealScore(IEntityManager ent, EntityUid owner, EntityUid item)
    {
        if (!ent.TryGetComponent(item, out HealingComponent? healing))
            return 0;

        if (ent.TryGetComponent(item, out StackComponent? stack) && stack.Count < 1)
            return 0;

        var healsBlunt = false;

        foreach (var (type, amount) in healing.Damage.DamageDict)
        {
            if (amount < 0 && type.ToString() == "Blunt")
                healsBlunt = true;
        }

        var stopsBleeding = healing.BloodlossModifier < 0f;
        var bandage = stopsBleeding && !healsBlunt;

        if (IsBleeding(ent, owner) && stopsBleeding)
            return bandage ? 1000 : 900;

        if (bandage)
            return 0;

        var damage = ent.System<DamageableSystem>().GetAllDamage(owner).DamageDict;
        var useful = 0f;

        foreach (var (type, amount) in healing.Damage.DamageDict)
        {
            if (amount < 0 && damage.TryGetValue(type, out var have) && have > 0)
                useful += -(float) amount;
        }

        return (int) useful;
    }

    public static EntityUid? FindHealItem(IEntityManager ent, EntityUid owner, HashSet<EntityUid>? ignored)
    {
        return FindHealItem(ent, owner, ignored, out _);
    }

    public static EntityUid? FindHealItem(IEntityManager ent, EntityUid owner, HashSet<EntityUid>? ignored, out int bestScore)
    {
        return FindHealItemFor(ent, owner, owner, ignored, out bestScore);
    }

    public static EntityUid? FindHealItemFor(IEntityManager ent, EntityUid medic, EntityUid patient, HashSet<EntityUid>? ignored, out int bestScore)
    {
        EntityUid? best = null;
        bestScore = 0;

        foreach (var item in ReserveItems(ent, medic))
        {
            if (ignored != null && ignored.Contains(item))
                continue;

            var score = HealScore(ent, patient, item);

            if (score <= bestScore)
                continue;

            best = item;
            bestScore = score;
        }

        return best;
    }

    public static HashSet<EntityUid>? IgnoredHealItems(NPCBlackboard blackboard, IEntityManager ent)
    {
        return blackboard.TryGetValue<HashSet<EntityUid>>(ADTHealSelfOperator.IgnoredKey, out var stored, ent)
            ? stored
            : null;
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

    private static void UnwieldHeld(IEntityManager ent, EntityUid owner, EntityUid except)
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
                StowSecondary(ent, owner, item);

            return hands.TrySetActiveHand(owner, found.Hand);
        }

        UnwieldHeld(ent, owner, item);

        if (!hands.TryGetEmptyHand(owner, out var emptyHand))
        {
            StowSecondary(ent, owner, item);

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
            Stow(ent, owner, old.Value);

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
            Stow(ent, owner, cartridge);

        return false;
    }
}

public sealed partial class ADTSwitchWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return ADTNpcWeaponHelper.SwitchTo(_entManager, owner, Role)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}

public sealed partial class ADTReloadWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string TimerKey = "ADTReloadTimer";

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    [DataField]
    public float MagazineDelay = 1f;

    [DataField]
    public float RoundDelay = 0.8f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (ADTNpcWeaponHelper.Resolve(_entManager, owner, Role) is not { } found)
            return HTNOperatorStatus.Failed;

        var gun = found.Item;
        ADTNpcWeaponHelper.GetAmmo(_entManager, gun, out var count, out var capacity);

        if (capacity <= 0 || count >= capacity)
            return HTNOperatorStatus.Finished;

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var stored, _entManager) ? stored : 0f) + frameTime;

        if (ADTNpcWeaponHelper.IsMagazineGun(_entManager, gun, out _))
        {
            if (timer < MagazineDelay)
            {
                blackboard.SetValue(TimerKey, timer);
                return HTNOperatorStatus.Continuing;
            }

            return ADTNpcWeaponHelper.SwapMagazine(_entManager, owner, gun)
                ? HTNOperatorStatus.Finished
                : HTNOperatorStatus.Failed;
        }

        if (!ADTNpcWeaponHelper.IsLooseGun(_entManager, gun))
            return HTNOperatorStatus.Failed;

        if (timer < RoundDelay)
        {
            blackboard.SetValue(TimerKey, timer);
            return HTNOperatorStatus.Continuing;
        }

        blackboard.SetValue(TimerKey, 0f);

        if (!ADTNpcWeaponHelper.LoadRound(_entManager, owner, gun))
            return count > 0 ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;

        return count + 1 >= capacity ? HTNOperatorStatus.Finished : HTNOperatorStatus.Continuing;
    }
}

public sealed partial class ADTWeaponPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    [DataField(required: true)]
    public ADTNpcWeaponCheck Check;

    [DataField]
    public bool Invert;

    [DataField]
    public bool Strict;

    private static GameTick _cacheTick;
    private static readonly Dictionary<(EntityUid, ADTNpcWeaponRole, bool, ADTNpcWeaponCheck), bool> Cache = new();

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_timing.CurTick != _cacheTick)
        {
            Cache.Clear();
            _cacheTick = _timing.CurTick;
        }

        var key = (owner, Role, Strict, Check);

        if (!Cache.TryGetValue(key, out var result))
        {
            result = Evaluate(owner);
            Cache[key] = result;
        }

        return result ^ Invert;
    }

    private bool Evaluate(EntityUid owner)
    {
        if (Check == ADTNpcWeaponCheck.ActiveNonWeapon)
        {
            return _entManager.System<SharedHandsSystem>().GetActiveItem(owner) is { } active &&
                   !_entManager.HasComponent<VirtualItemComponent>(active) &&
                   ADTNpcWeaponHelper.Classify(_entManager, active) == null;
        }

        if (ADTNpcWeaponHelper.Resolve(_entManager, owner, Role, Strict) is not { } found)
            return false;

        var gun = found.Item;
        var melee = Role == ADTNpcWeaponRole.Melee;

        switch (Check)
        {
            case ADTNpcWeaponCheck.Exists:
                return true;
            case ADTNpcWeaponCheck.Held:
                return found.Hand != null;
            case ADTNpcWeaponCheck.Active:
                return _entManager.System<SharedHandsSystem>().GetActiveItem(owner) == gun;
        }

        if (melee)
            return Check is ADTNpcWeaponCheck.HasAmmo or ADTNpcWeaponCheck.Usable;

        ADTNpcWeaponHelper.GetAmmo(_entManager, gun, out var count, out var capacity);

        switch (Check)
        {
            case ADTNpcWeaponCheck.HasAmmo:
                return count > 0;
            case ADTNpcWeaponCheck.Empty:
                return found.Hand != null && count <= 0;
            case ADTNpcWeaponCheck.NotFull:
                return capacity > 0 && count < capacity;
            case ADTNpcWeaponCheck.CanReload:
                return capacity > 0 && count < capacity && ADTNpcWeaponHelper.HasReserve(_entManager, owner, gun);
            case ADTNpcWeaponCheck.Usable:
                return count > 0 || (capacity > 0 && ADTNpcWeaponHelper.HasReserve(_entManager, owner, gun));
            case ADTNpcWeaponCheck.Magazine:
                return ADTNpcWeaponHelper.IsMagazineGun(_entManager, gun, out _);
            case ADTNpcWeaponCheck.Loose:
                return ADTNpcWeaponHelper.IsLooseGun(_entManager, gun);
            default:
                return false;
        }
    }
}

public sealed partial class ADTStandUpOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string WaitKey = "ADTStandWait";
    private const string TotalKey = "ADTStandTotal";

    [DataField]
    public float RetryDelay = 4f;

    [DataField]
    public float Timeout = 20f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(WaitKey, 0f);
        blackboard.SetValue(TotalKey, 0f);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent(owner, out StandingStateComponent? standing) || standing.Standing)
            return HTNOperatorStatus.Finished;

        var total = (blackboard.TryGetValue<float>(TotalKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TotalKey, total);

        if (total > Timeout)
            return HTNOperatorStatus.Failed;

        var wait = blackboard.TryGetValue<float>(WaitKey, out var w, _entManager) ? w : 0f;

        if (wait > 0f)
        {
            blackboard.SetValue(WaitKey, wait - frameTime);
            return HTNOperatorStatus.Continuing;
        }

        if (_entManager.TryGetComponent(owner, out KnockedDownComponent? knocked))
        {
            if (_entManager.System<SharedStunSystem>().TryStanding((owner, knocked)))
                blackboard.SetValue(WaitKey, RetryDelay);
        }
        else
        {
            if (!_entManager.System<StandingStateSystem>().Stand(owner))
                blackboard.SetValue(WaitKey, 1f);
        }

        return HTNOperatorStatus.Continuing;
    }
}

public sealed partial class ADTDownedPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _entManager.TryGetComponent(owner, out StandingStateComponent? standing) && !standing.Standing;
    }
}

public sealed partial class ADTFindWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public const string IgnoredKey = "ADTIgnoredWeapons";

    [DataField]
    public float Range = 7f;

    [DataField]
    public ADTNpcWeaponRole? OnlyRole;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var lookup = _entManager.System<EntityLookupSystem>();
        var transform = _entManager.System<SharedTransformSystem>();
        var containers = _entManager.System<SharedContainerSystem>();

        var ignored = blackboard.TryGetValue<HashSet<EntityUid>>(IgnoredKey, out var stored, _entManager)
            ? stored
            : new HashSet<EntityUid>();

        var ownerPos = transform.GetMapCoordinates(owner).Position;
        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;

        EntityUid? best = null;
        var bestScore = float.MaxValue;

        var candidates = new List<EntityUid>();

        if (OnlyRole == ADTNpcWeaponRole.Melee)
        {
            foreach (var found in lookup.GetEntitiesInRange<ItemComponent>(ownerCoords, Range))
                candidates.Add(found.Owner);
        }
        else
        {
            foreach (var found in lookup.GetEntitiesInRange<GunComponent>(ownerCoords, Range))
                candidates.Add(found.Owner);
        }

        foreach (var gun in candidates)
        {
            if (ignored.Contains(gun) || !_entManager.HasComponent<ItemComponent>(gun))
                continue;

            if (containers.IsEntityInContainer(gun))
                continue;

            var role = ADTNpcWeaponHelper.Classify(_entManager, gun);

            if (OnlyRole != null && role != OnlyRole)
                continue;

            if (OnlyRole != ADTNpcWeaponRole.Melee)
            {
                ADTNpcWeaponHelper.GetAmmo(_entManager, gun, out var count, out _);

                if (count <= 0)
                    continue;
            }

            var score = Vector2.Distance(ownerPos, transform.GetMapCoordinates(gun).Position);

            if (role != ADTNpcWeaponRole.Main)
                score += 100f;

            if (score >= bestScore)
                continue;

            best = gun;
            bestScore = score;
        }

        if (best is not { } target)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
        }));
    }
}

public sealed partial class ADTPickupWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 2.5f;

    private const string TimerKey = "ADTPickupTimer";

    [DataField]
    public float RoomTimeout = 3f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
    }

    private void Ignore(NPCBlackboard blackboard, EntityUid target)
    {
        var ignored = blackboard.TryGetValue<HashSet<EntityUid>>(ADTFindWeaponOperator.IgnoredKey, out var stored, _entManager)
            ? stored
            : new HashSet<EntityUid>();

        ignored.Add(target);
        blackboard.SetValue(ADTFindWeaponOperator.IgnoredKey, ignored);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        var hands = _entManager.System<SharedHandsSystem>();
        var transform = _entManager.System<SharedTransformSystem>();

        var near = Vector2.Distance(
            transform.GetMapCoordinates(owner).Position,
            transform.GetMapCoordinates(target).Position) <= MaxDistance;

        if (!near)
        {
            Ignore(blackboard, target);
            return HTNOperatorStatus.Failed;
        }

        if (!ADTNpcWeaponHelper.MakeRoom(_entManager, owner, out var hand) || hand == null)
        {
            var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
            blackboard.SetValue(TimerKey, timer);

            if (timer < RoomTimeout)
                return HTNOperatorStatus.Continuing;

            Ignore(blackboard, target);
            return HTNOperatorStatus.Failed;
        }

        if (hands.TryPickup(owner, target, hand, checkActionBlocker: false, animate: false))
        {
            hands.TrySetActiveHand(owner, hand);
            return HTNOperatorStatus.Finished;
        }

        Ignore(blackboard, target);
        return HTNOperatorStatus.Failed;
    }
}

public sealed partial class ADTHealthPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Below = 0.4f;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var health = 1f - ADTNpcWeaponHelper.DamageFraction(_entManager, owner);

        return (health < Below) ^ Invert;
    }
}

public sealed partial class ADTNeedHealPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float MinDamage = 0.1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var hurt = ADTNpcWeaponHelper.IsBleeding(_entManager, owner)
            || ADTNpcWeaponHelper.DamageFraction(_entManager, owner) >= MinDamage;

        return hurt && ADTNpcWeaponHelper.FindHealItem(
            _entManager, owner, ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager)) != null;
    }
}

public sealed partial class ADTHostileNearPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Range = 10f;

    [DataField]
    public bool Invert;

    private static GameTick _cacheTick;
    private static readonly Dictionary<(EntityUid, float), bool> Cache = new();

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_timing.CurTick != _cacheTick)
        {
            Cache.Clear();
            _cacheTick = _timing.CurTick;
        }

        if (!Cache.TryGetValue((owner, Range), out var near))
        {
            var mobState = _entManager.System<MobStateSystem>();

            foreach (var hostile in _entManager.System<NpcFactionSystem>().GetNearbyHostiles(owner, Range))
            {
                if (mobState.IsIncapacitated(hostile))
                    continue;

                near = true;
                break;
            }

            Cache[(owner, Range)] = near;
        }

        return near ^ Invert;
    }
}

public sealed partial class ADTHealSelfOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public const string IgnoredKey = "ADTIgnoredHealItems";
    private const string ItemKey = "ADTHealItem";
    private const string TimerKey = "ADTHealTimer";
    private const string StartedKey = "ADTHealStarted";

    [DataField]
    public float RoomTimeout = 3f;

    [DataField]
    public float DoAfterTimeout = 20f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.SetValue(StartedKey, false);
        blackboard.Remove<EntityUid>(ItemKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (blackboard.TryGetValue<EntityUid>(ItemKey, out var item, _entManager) && !_entManager.Deleted(item))
            ADTNpcWeaponHelper.Stow(_entManager, owner, item);

        blackboard.Remove<EntityUid>(ItemKey);
    }

    private void Ignore(NPCBlackboard blackboard, EntityUid item)
    {
        var ignored = ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager) ?? new HashSet<EntityUid>();
        ignored.Add(item);
        blackboard.SetValue(IgnoredKey, ignored);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var hands = _entManager.System<SharedHandsSystem>();

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var stored, _entManager) ? stored : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        var started = blackboard.TryGetValue<bool>(StartedKey, out var flag, _entManager) && flag;

        if (!started)
        {
            if (ADTNpcWeaponHelper.FindHealItem(_entManager, owner, ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager))
                is not { } item)
            {
                return HTNOperatorStatus.Failed;
            }

            if (!ADTNpcWeaponHelper.MakeRoom(_entManager, owner, out var hand) || hand == null)
                return timer < RoomTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

            if (!hands.TryPickup(owner, item, hand, checkActionBlocker: false, animate: false))
            {
                Ignore(blackboard, item);
                return HTNOperatorStatus.Failed;
            }

            hands.TrySetActiveHand(owner, hand);
            blackboard.SetValue(ItemKey, item);

            _entManager.System<SharedInteractionSystem>().UseInHandInteraction(owner, item);

            if (!_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            {
                Ignore(blackboard, item);
                return HTNOperatorStatus.Failed;
            }

            blackboard.SetValue(StartedKey, true);
            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        if (!blackboard.TryGetValue<EntityUid>(ItemKey, out var current, _entManager) || _entManager.Deleted(current))
            return HTNOperatorStatus.Finished;

        var currentScore = ADTNpcWeaponHelper.HealScore(_entManager, owner, current);
        ADTNpcWeaponHelper.FindHealItem(_entManager, owner, ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager), out var bestScore);

        if (currentScore <= 0 || currentScore < bestScore)
        {
            ADTNpcWeaponHelper.Stow(_entManager, owner, current);
            blackboard.Remove<EntityUid>(ItemKey);
            return HTNOperatorStatus.Finished;
        }

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < DoAfterTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}

public static class ADTNpcMedicHelper
{
    public const string ActionKey = "ADTMedAction";
    public const string ActionHeal = "Heal";
    public const string ActionMedipen = "Medipen";
    public const string ActionDefib = "Defib";
    public const string MedipenTimesKey = "ADTMedipenTimes";

    public static readonly string[] Medipens = { "CombatMedipen", "EmergencyMedipen" };

    public static EntityUid? FindMedipen(IEntityManager ent, EntityUid medic)
    {
        foreach (var item in ADTNpcWeaponHelper.ReserveItems(ent, medic))
        {
            var id = ent.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID;

            if (id != null && Medipens.Contains(id))
                return item;
        }

        return null;
    }

    public static EntityUid? FindDefib(IEntityManager ent, EntityUid medic)
    {
        var inv = ent.System<InventorySystem>();

        if (inv.TryGetSlotEntity(medic, "suitstorage", out var worn) && ent.HasComponent<DefibrillatorComponent>(worn.Value))
            return HasCharge(ent, worn.Value) ? worn : null;

        foreach (var (_, held) in ADTNpcWeaponHelper.HeldItems(ent, medic))
        {
            if (ent.HasComponent<DefibrillatorComponent>(held))
                return HasCharge(ent, held) ? held : null;
        }

        return null;
    }

    private static bool HasCharge(IEntityManager ent, EntityUid defib)
    {
        return ent.System<PowerCellSystem>().HasActivatableCharge(defib);
    }

    public static bool CanBeRevived(IEntityManager ent, EntityUid patient)
    {
        if (ent.HasComponent<UnrevivableComponent>(patient))
            return false;

        return !ent.System<SharedRottingSystem>().IsRotten(patient);
    }

    public static bool RevivableByZap(IEntityManager ent, EntityUid patient, EntityUid defib)
    {
        if (!ent.System<MobThresholdSystem>().TryGetThresholdForState(patient, MobState.Dead, out var threshold))
            return false;

        if (!ent.TryGetComponent(defib, out DefibrillatorComponent? defibComp))
            return false;

        var damageable = ent.System<DamageableSystem>();
        var damage = damageable.GetAllDamage(patient).DamageDict;
        var total = (float) damageable.GetTotalDamage(patient);

        foreach (var (type, amount) in defibComp.ZapHeal.DamageDict)
        {
            if (amount >= 0 || !damage.TryGetValue(type, out var have) || have <= 0)
                continue;

            total -= MathF.Min((float) have, -(float) amount);
        }

        return total < (float) threshold.Value - 1f;
    }

    public static bool MedipenRecentlyUsed(NPCBlackboard blackboard, IEntityManager ent, EntityUid patient, TimeSpan now, float cooldown)
    {
        if (!blackboard.TryGetValue<Dictionary<EntityUid, TimeSpan>>(MedipenTimesKey, out var times, ent))
            return false;

        return times.TryGetValue(patient, out var at) && (now - at).TotalSeconds < cooldown;
    }

    public static void MarkMedipen(NPCBlackboard blackboard, IEntityManager ent, EntityUid patient, TimeSpan now)
    {
        var times = blackboard.TryGetValue<Dictionary<EntityUid, TimeSpan>>(MedipenTimesKey, out var stored, ent)
            ? stored
            : new Dictionary<EntityUid, TimeSpan>();

        times[patient] = now;
        blackboard.SetValue(MedipenTimesKey, times);
    }
}

public sealed partial class ADTFindPatientOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Range = 9f;

    [DataField]
    public float MinDamage = 0.1f;

    [DataField]
    public float MedipenCooldown = 30f;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var lookup = _entManager.System<EntityLookupSystem>();
        var transform = _entManager.System<SharedTransformSystem>();
        var faction = _entManager.System<NpcFactionSystem>();
        var mobState = _entManager.System<MobStateSystem>();
        var ignored = ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager);

        var ownerMap = transform.GetMapCoordinates(owner);
        var defib = ADTNpcMedicHelper.FindDefib(_entManager, owner);
        var hasMedipen = ADTNpcMedicHelper.FindMedipen(_entManager, owner) != null;

        EntityUid? best = null;
        string? bestAction = null;
        var bestScore = float.MinValue;

        foreach (var found in lookup.GetEntitiesInRange<NpcFactionMemberComponent>(ownerMap, Range))
        {
            var patient = found.Owner;

            if (patient == owner || !_entManager.HasComponent<MobStateComponent>(patient))
                continue;

            if (!faction.IsEntityFriendly(owner, patient))
                continue;

            var canHeal = ADTNpcWeaponHelper.FindHealItemFor(_entManager, owner, patient, ignored, out _) != null;
            string? action = null;
            var priority = 0f;

            if (mobState.IsDead(patient))
            {
                if (!ADTNpcMedicHelper.CanBeRevived(_entManager, patient))
                    continue;

                if (defib != null && ADTNpcMedicHelper.RevivableByZap(_entManager, patient, defib.Value))
                {
                    action = ADTNpcMedicHelper.ActionDefib;
                    priority = 30f;
                }
                else if (canHeal)
                {
                    action = ADTNpcMedicHelper.ActionHeal;
                    priority = 25f;
                }
            }
            else if (mobState.IsCritical(patient))
            {
                if (hasMedipen && !ADTNpcMedicHelper.MedipenRecentlyUsed(blackboard, _entManager, patient, _timing.CurTime, MedipenCooldown))
                {
                    action = ADTNpcMedicHelper.ActionMedipen;
                    priority = 40f;
                }
                else if (canHeal)
                {
                    action = ADTNpcMedicHelper.ActionHeal;
                    priority = 35f;
                }
            }
            else if (canHeal)
            {
                var bleeding = ADTNpcWeaponHelper.IsBleeding(_entManager, patient);

                if (bleeding || ADTNpcWeaponHelper.DamageFraction(_entManager, patient) >= MinDamage)
                {
                    action = ADTNpcMedicHelper.ActionHeal;
                    priority = bleeding ? 15f : 10f;
                }
            }

            if (action == null)
                continue;

            var score = priority - Vector2.Distance(ownerMap.Position, transform.GetMapCoordinates(patient).Position) * 0.1f;

            if (score <= bestScore)
                continue;

            best = patient;
            bestAction = action;
            bestScore = score;
        }

        if (best is not { } target || bestAction == null)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
            { ADTNpcMedicHelper.ActionKey, bestAction },
        }));
    }
}

public sealed partial class ADTTreatPatientOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const string ItemKey = "ADTTreatItem";
    private const string TimerKey = "ADTTreatTimer";
    private const string StartedKey = "ADTTreatStarted";

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 2f;

    [DataField]
    public float StartTimeout = 3f;

    [DataField]
    public float DoAfterTimeout = 20f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.SetValue(StartedKey, false);
        blackboard.Remove<EntityUid>(ItemKey);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        Release(blackboard);
    }

    private void Release(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (blackboard.TryGetValue<EntityUid>(ItemKey, out var item, _entManager) && !_entManager.Deleted(item))
        {
            var id = _entManager.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID;

            if (id != null && ADTNpcMedicHelper.Medipens.Contains(id))
                _entManager.System<SharedHandsSystem>().TryDrop(owner, item, checkActionBlocker: false, doDropInteraction: false);
            else
                ADTNpcWeaponHelper.Stow(_entManager, owner, item);
        }

        blackboard.Remove<EntityUid>(ItemKey);
    }

    private bool TakeInHand(EntityUid owner, EntityUid item)
    {
        var hands = _entManager.System<SharedHandsSystem>();

        if (!ADTNpcWeaponHelper.MakeRoom(_entManager, owner, out var hand) || hand == null)
            return false;

        if (!hands.TryPickup(owner, item, hand, checkActionBlocker: false, animate: false))
            return false;

        hands.TrySetActiveHand(owner, hand);
        return true;
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var patient, _entManager) || _entManager.Deleted(patient))
            return HTNOperatorStatus.Failed;

        if (!blackboard.TryGetValue<string>(ADTNpcMedicHelper.ActionKey, out var action, _entManager))
            return HTNOperatorStatus.Failed;

        var transform = _entManager.System<SharedTransformSystem>();

        if (Vector2.Distance(transform.GetMapCoordinates(owner).Position, transform.GetMapCoordinates(patient).Position) > MaxDistance)
            return HTNOperatorStatus.Failed;

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        var started = blackboard.TryGetValue<bool>(StartedKey, out var flag, _entManager) && flag;

        if (!started)
        {
            _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false);
            var patientCoords = _entManager.GetComponent<TransformComponent>(patient).Coordinates;

            if (action == ADTNpcMedicHelper.ActionDefib)
            {
                if (ADTNpcMedicHelper.FindDefib(_entManager, owner) is not { } defib)
                    return HTNOperatorStatus.Failed;

                var toggle = _entManager.System<ItemToggleSystem>();

                if (!toggle.IsActivated(defib))
                    toggle.TryActivate(defib, owner);

                if (!_entManager.System<SharedDefibrillatorSystem>().TryStartZap(defib, patient, owner))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;
            }
            else
            {
                EntityUid? item = action == ADTNpcMedicHelper.ActionMedipen
                    ? ADTNpcMedicHelper.FindMedipen(_entManager, owner)
                    : ADTNpcWeaponHelper.FindHealItemFor(_entManager, owner, patient,
                        ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager), out _);

                if (item == null)
                    return HTNOperatorStatus.Failed;

                if (!TakeInHand(owner, item.Value))
                    return timer < StartTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

                blackboard.SetValue(ItemKey, item.Value);
                _entManager.System<SharedInteractionSystem>().UserInteraction(owner, patientCoords, patient);

                if (action == ADTNpcMedicHelper.ActionMedipen)
                    ADTNpcMedicHelper.MarkMedipen(blackboard, _entManager, patient, _timing.CurTime);
            }

            blackboard.SetValue(StartedKey, true);
            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        if (action == ADTNpcMedicHelper.ActionHeal &&
            blackboard.TryGetValue<EntityUid>(ItemKey, out var current, _entManager) &&
            !_entManager.Deleted(current))
        {
            var currentScore = ADTNpcWeaponHelper.HealScore(_entManager, patient, current);
            ADTNpcWeaponHelper.FindHealItemFor(_entManager, owner, patient,
                ADTNpcWeaponHelper.IgnoredHealItems(blackboard, _entManager), out var bestScore);

            if (currentScore <= 0 || currentScore < bestScore)
            {
                Release(blackboard);
                return HTNOperatorStatus.Finished;
            }
        }

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < DoAfterTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        if (action == ADTNpcMedicHelper.ActionDefib && ADTNpcMedicHelper.FindDefib(_entManager, owner) is { } used)
            _entManager.System<ItemToggleSystem>().TryDeactivate(used, owner);

        Release(blackboard);
        return HTNOperatorStatus.Finished;
    }
}

[RegisterComponent]
public sealed partial class ADTNpcCombatMemoryComponent : Component
{
    [ViewVariables]
    public bool Hurt;

    [ViewVariables]
    public TimeSpan LastHurt;

    [ViewVariables]
    public bool Blocked;

    [ViewVariables]
    public TimeSpan LastBlocked;

    [DataField]
    public float AllyClearance = 0.7f;

    [ViewVariables]
    public bool Spoke;

    [ViewVariables]
    public TimeSpan LastSpoke;
}

public sealed class ADTNpcCombatMemorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(EntityUid uid, ADTNpcCombatMemoryComponent component, EntitySpokeEvent args)
    {
        component.Spoke = true;
        component.LastSpoke = _timing.CurTime;
    }

    private void OnShotAttempted(EntityUid uid, ADTNpcCombatMemoryComponent component, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || !HasComp<ActiveNPCComponent>(uid))
            return;

        if (args.Used.Comp.ShootCoordinates is not { } coordinates)
            return;

        var from = _transform.GetMapCoordinates(uid);
        var to = _transform.ToMapCoordinates(coordinates);

        if (from.MapId != to.MapId)
            return;

        var segment = to.Position - from.Position;
        var lengthSq = segment.LengthSquared();

        if (lengthSq < 0.01f)
            return;

        foreach (var found in _lookup.GetEntitiesInRange<NpcFactionMemberComponent>(from, MathF.Sqrt(lengthSq) + 0.5f))
        {
            var ally = found.Owner;

            if (ally == uid || _mobState.IsIncapacitated(ally) || !_faction.IsEntityFriendly(uid, ally))
                continue;

            var pos = _transform.GetMapCoordinates(ally).Position;
            var along = Vector2.Dot(pos - from.Position, segment) / lengthSq;

            if (along <= 0f || along >= 1f)
                continue;

            if (Vector2.Distance(from.Position + segment * along, pos) > component.AllyClearance)
                continue;

            args.Cancel();
            component.Blocked = true;
            component.LastBlocked = _timing.CurTime;
            return;
        }
    }

    private void OnDamageChanged(EntityUid uid, ADTNpcCombatMemoryComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin == null || args.Origin == uid)
            return;

        component.Hurt = true;
        component.LastHurt = _timing.CurTime;
    }
}

public sealed partial class ADTRecentlyHurtPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Seconds = 2f;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var memory = _entManager.EnsureComponent<ADTNpcCombatMemoryComponent>(owner);
        var hurt = memory.Hurt && (_timing.CurTime - memory.LastHurt).TotalSeconds < Seconds;

        return hurt ^ Invert;
    }
}

public sealed partial class ADTWaitOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string TimerKey = "ADTWaitTimer";

    [DataField]
    public float Seconds = 2f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, Seconds);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) - frameTime;
        blackboard.SetValue(TimerKey, timer);

        return timer <= 0f ? HTNOperatorStatus.Finished : HTNOperatorStatus.Continuing;
    }
}

public sealed partial class ADTPickCoverOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private PathfindingSystem _pathfinding = default!;
    private SharedTransformSystem _transform = default!;
    private SharedInteractionSystem _interaction = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    [DataField]
    public int Radius = 4;

    [DataField]
    public int Attempts = 6;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return (false, null);

        var ownerPos = _transform.GetMapCoordinates(owner);
        var targetPos = _transform.GetMapCoordinates(target);

        if (ownerPos.MapId != targetPos.MapId)
            return (false, null);

        var candidates = new List<(Vector2 Pos, float Dist)>();

        for (var x = -Radius; x <= Radius; x++)
        {
            for (var y = -Radius; y <= Radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                var offset = new Vector2(x, y);
                var dist = offset.Length();

                if (dist > Radius)
                    continue;

                var point = new MapCoordinates(ownerPos.Position + offset, ownerPos.MapId);

                if (!_interaction.InRangeUnobstructed(ownerPos, point, Radius + 1f, predicate: e => e == owner || e == target))
                    continue;

                if (_interaction.InRangeUnobstructed(targetPos, point, 40f, predicate: e => e == target || e == owner))
                    continue;

                candidates.Add((point.Position, dist));
            }
        }

        candidates.Sort((a, b) => a.Dist.CompareTo(b.Dist));

        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;
        var tries = 0;

        foreach (var (pos, _) in candidates)
        {
            if (tries++ >= Attempts)
                break;

            var destination = new MapCoordinates(pos, ownerPos.MapId);
            var destinationCoords = ownerCoords.EntityId.IsValid()
                ? _transform.ToCoordinates(ownerCoords.EntityId, destination)
                : _transform.ToCoordinates(destination);

            var path = await _pathfinding.GetPath(
                owner,
                ownerCoords,
                destinationCoords,
                0.5f,
                cancelToken,
                flags: _pathfinding.GetFlags(blackboard));

            if (path.Result != PathResult.Path || path.Path.Count == 0)
                continue;

            return (true, new Dictionary<string, object>
            {
                { TargetCoordinates, destinationCoords },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }
}

public sealed partial class ADTFindObstacleOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public const string ObstacleKey = "ADTObstacle";
    public const string ObstacleCoordinatesKey = "ADTObstacleCoordinates";

    [DataField]
    public float Range = 10f;

    [DataField]
    public float LineWidth = 0.7f;

    [DataField]
    public bool Openable;

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var transform = _entManager.System<SharedTransformSystem>();
        var interaction = _entManager.System<SharedInteractionSystem>();
        var lookup = _entManager.System<EntityLookupSystem>();
        var containers = _entManager.System<SharedContainerSystem>();
        var storageSystem = _entManager.System<SharedEntityStorageSystem>();
        var mobState = _entManager.System<MobStateSystem>();

        var ownerMap = transform.GetMapCoordinates(owner);
        EntityUid? best = null;
        var bestDist = float.MaxValue;

        foreach (var hostile in _entManager.System<NpcFactionSystem>().GetNearbyHostiles(owner, Range))
        {
            if (mobState.IsIncapacitated(hostile))
                continue;

            if (containers.TryGetContainingContainer(hostile, out var container))
            {
                if (!_entManager.HasComponent<EntityStorageComponent>(container.Owner))
                    continue;

                var canOpen = storageSystem.CanOpen(owner, container.Owner, true);

                if (canOpen != Openable)
                    continue;

                var d = Vector2.Distance(ownerMap.Position, transform.GetMapCoordinates(container.Owner).Position);

                if (d < bestDist)
                {
                    best = container.Owner;
                    bestDist = d;
                }

                continue;
            }

            if (Openable)
                continue;

            var hostileMap = transform.GetMapCoordinates(hostile);

            if (hostileMap.MapId != ownerMap.MapId)
                continue;

            if (interaction.InRangeUnobstructed(ownerMap, hostileMap, Range + 1f, predicate: e => e == owner || e == hostile))
                continue;

            var start = ownerMap.Position;
            var end = hostileMap.Position;
            var segment = end - start;
            var lengthSq = segment.LengthSquared();

            if (lengthSq < 0.01f)
                continue;

            foreach (var storage in lookup.GetEntitiesInRange<EntityStorageComponent>(ownerMap, Range))
            {
                var pos = transform.GetMapCoordinates(storage.Owner).Position;
                var along = Vector2.Dot(pos - start, segment) / lengthSq;

                if (along <= 0f || along >= 1f)
                    continue;

                var closest = start + segment * along;

                if (Vector2.Distance(closest, pos) > LineWidth)
                    continue;

                var blocker = storage.Owner;

                if (!interaction.InRangeUnobstructed(ownerMap, hostileMap, Range + 1f,
                        predicate: e => e == owner || e == hostile || e == blocker))
                {
                    continue;
                }

                var d = Vector2.Distance(start, pos);

                if (d < bestDist)
                {
                    best = storage.Owner;
                    bestDist = d;
                }
            }
        }

        if (best is not { } obstacle)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { ObstacleKey, obstacle },
            { ObstacleCoordinatesKey, new EntityCoordinates(obstacle, Vector2.Zero) },
        }));
    }
}

public sealed partial class ADTOpenStorageOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public string Key = ADTFindObstacleOperator.ObstacleKey;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var storage, _entManager) || _entManager.Deleted(storage))
            return HTNOperatorStatus.Failed;

        return _entManager.System<SharedEntityStorageSystem>().TryOpenStorage(owner, storage)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}

public sealed partial class ADTRandomChancePrecondition : HTNPrecondition
{
    [Dependency] private IRobustRandom _random = default!;

    [DataField]
    public float Chance = 0.01f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return _random.Prob(Chance);
    }
}

public static class ADTNpcGroupHelper
{
    public static EntityUid FindLeader(IEntityManager ent, EntityUid owner, float range)
    {
        var lookup = ent.System<EntityLookupSystem>();
        var transform = ent.System<SharedTransformSystem>();
        var faction = ent.System<NpcFactionSystem>();
        var mobState = ent.System<MobStateSystem>();

        var leader = owner;

        foreach (var found in lookup.GetEntitiesInRange<NpcFactionMemberComponent>(transform.GetMapCoordinates(owner), range))
        {
            var other = found.Owner;

            if (other == owner || !ent.HasComponent<HTNComponent>(other))
                continue;

            if (!mobState.IsAlive(other) || !faction.IsEntityFriendly(owner, other))
                continue;

            if (other.CompareTo(leader) < 0)
                leader = other;
        }

        return leader;
    }
}

public sealed partial class ADTIsGroupLeaderPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Range = 12f;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return (ADTNpcGroupHelper.FindLeader(_entManager, owner, Range) == owner) ^ Invert;
    }
}

public sealed partial class ADTFollowLeaderOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Range = 12f;

    [DataField]
    public float FollowDistance = 4f;

    [DataField]
    public string Key = "ADTLeader";

    [DataField]
    public string KeyCoordinates = "ADTLeaderCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var leader = ADTNpcGroupHelper.FindLeader(_entManager, owner, Range);

        if (leader == owner)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        var transform = _entManager.System<SharedTransformSystem>();
        var distance = Vector2.Distance(
            transform.GetMapCoordinates(owner).Position,
            transform.GetMapCoordinates(leader).Position);

        if (distance <= FollowDistance)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, leader },
            { KeyCoordinates, new EntityCoordinates(leader, Vector2.Zero) },
        }));
    }
}

public sealed partial class ADTEatOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string ItemKey = "ADTEatItem";
    private const string HandKey = "ADTEatHand";
    private const string TimerKey = "ADTEatTimer";
    private const string TotalKey = "ADTEatTotal";
    private const string MaskKey = "ADTEatMaskDown";

    [DataField]
    public float BiteInterval = 1.5f;

    [DataField]
    public float BiteTimeout = 10f;

    [DataField]
    public float TotalTimeout = 90f;

    public static EntityUid? FindFood(IEntityManager ent, EntityUid owner)
    {
        foreach (var item in ADTNpcWeaponHelper.ReserveItems(ent, owner))
        {
            if (ent.HasComponent<EdibleComponent>(item))
                return item;
        }

        return null;
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
        blackboard.SetValue(TotalKey, 0f);
        blackboard.Remove<EntityUid>(ItemKey);
        blackboard.Remove<string>(HandKey);
        blackboard.SetValue(MaskKey, false);
    }

    private EntityUid? GetMask(EntityUid owner)
    {
        if (!_entManager.System<InventorySystem>().TryGetSlotEntity(owner, "mask", out var mask))
            return null;

        return _entManager.HasComponent<MaskComponent>(mask.Value) ? mask : null;
    }

    private void SetMask(EntityUid owner, EntityUid mask, bool down)
    {
        var args = new DoAfterArgs(_entManager, owner, TimeSpan.Zero, new ToggleMaskDoAfterEvent(down, false, false), mask, target: owner);
        _entManager.System<SharedDoAfterSystem>().TryStartDoAfter(args);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var hands = _entManager.System<SharedHandsSystem>();

        if (blackboard.TryGetValue<string>(HandKey, out var hand, _entManager) &&
            hands.TryGetHeldItem(owner, hand, out var held, true) &&
            ADTNpcWeaponHelper.Classify(_entManager, held.Value) == null)
        {
            if (_entManager.HasComponent<EdibleComponent>(held.Value))
                ADTNpcWeaponHelper.Stow(_entManager, owner, held.Value);
            else
                hands.TryDrop(owner, held.Value, checkActionBlocker: false, doDropInteraction: false);
        }

        blackboard.Remove<EntityUid>(ItemKey);
        blackboard.Remove<string>(HandKey);

        if (blackboard.TryGetValue<bool>(MaskKey, out var lowered, _entManager) && lowered && GetMask(owner) is { } worn)
            SetMask(owner, worn, false);

        blackboard.SetValue(MaskKey, false);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var hands = _entManager.System<SharedHandsSystem>();

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        if (GetMask(owner) is { } mask &&
            _entManager.TryGetComponent(mask, out MaskComponent? maskComp) &&
            !maskComp.IsToggled &&
            _entManager.HasComponent<IngestionBlockerComponent>(mask))
        {
            if (!maskComp.IsToggleable)
                return HTNOperatorStatus.Failed;

            if (!blackboard.TryGetValue<bool>(MaskKey, out var requested, _entManager) || !requested)
            {
                SetMask(owner, mask, true);
                blackboard.SetValue(MaskKey, true);
            }

            return timer < 3f ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;
        }

        if (!blackboard.TryGetValue<EntityUid>(ItemKey, out var food, _entManager))
        {
            if (FindFood(_entManager, owner) is not { } found)
                return HTNOperatorStatus.Failed;

            if (!ADTNpcWeaponHelper.MakeRoom(_entManager, owner, out var freeHand) || freeHand == null)
                return timer < 3f ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

            if (!hands.TryPickup(owner, found, freeHand, checkActionBlocker: false, animate: false))
                return HTNOperatorStatus.Failed;

            hands.TrySetActiveHand(owner, freeHand);
            blackboard.SetValue(ItemKey, found);
            blackboard.SetValue(HandKey, freeHand);

            if (_entManager.TryGetComponent(found, out OpenableComponent? openable))
                _entManager.System<OpenableSystem>().TryOpen(found, openable, owner);

            blackboard.SetValue(TimerKey, 0f);
            return HTNOperatorStatus.Continuing;
        }

        var total = (blackboard.TryGetValue<float>(TotalKey, out var tt, _entManager) ? tt : 0f) + frameTime;
        blackboard.SetValue(TotalKey, total);

        if (total > TotalTimeout)
            return HTNOperatorStatus.Finished;

        if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
            return timer < BiteTimeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

        if (_entManager.Deleted(food) || !_entManager.HasComponent<EdibleComponent>(food))
            return HTNOperatorStatus.Finished;

        if (timer < BiteInterval)
            return HTNOperatorStatus.Continuing;

        blackboard.SetValue(TimerKey, 0f);
        _entManager.System<SharedInteractionSystem>().UseInHandInteraction(owner, food, checkUseDelay: false);

        return HTNOperatorStatus.Continuing;
    }
}

public sealed partial class ADTCanEatPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_entManager.TryGetComponent(owner, out InternalsComponent? internals) && internals.GasTankEntity != null)
            return false;

        return ADTEatOperator.FindFood(_entManager, owner) != null;
    }
}

public sealed class ADTRandomTTSSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTRandomTTSComponent, MapInitEvent>(OnMapInit, after: new[] { typeof(RandomHumanoidAppearanceSystem), typeof(ADTRandomHairSystem) });
    }

    private void OnMapInit(EntityUid uid, ADTRandomTTSComponent component, MapInitEvent args)
    {
        if (component.Voices.Count == 0)
            return;

        var tts = EnsureComp<TTSComponent>(uid);
        tts.VoicePrototypeId = _random.Pick(component.Voices);
        Dirty(uid, tts);
    }
}

public sealed partial class ADTAllyInLineOfFirePrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Seconds = 1f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent(owner, out ADTNpcCombatMemoryComponent? memory) || !memory.Blocked)
            return false;

        return (_timing.CurTime - memory.LastBlocked).TotalSeconds < Seconds;
    }
}

public sealed partial class ADTSidestepOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private PathfindingSystem _pathfinding = default!;
    private SharedTransformSystem _transform = default!;

    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return (false, null);

        var ownerPos = _transform.GetMapCoordinates(owner);
        var targetPos = _transform.GetMapCoordinates(target);

        if (ownerPos.MapId != targetPos.MapId)
            return (false, null);

        var dir = targetPos.Position - ownerPos.Position;

        if (dir.LengthSquared() < 0.01f)
            return (false, null);

        dir = Vector2.Normalize(dir);
        var side = new Vector2(-dir.Y, dir.X);
        var ownerCoords = _entManager.GetComponent<TransformComponent>(owner).Coordinates;

        foreach (var offset in new[] { 1.5f, -1.5f, 2.5f, -2.5f })
        {
            var destination = new MapCoordinates(ownerPos.Position + side * offset, ownerPos.MapId);
            var destinationCoords = ownerCoords.EntityId.IsValid()
                ? _transform.ToCoordinates(ownerCoords.EntityId, destination)
                : _transform.ToCoordinates(destination);

            var path = await _pathfinding.GetPath(
                owner,
                ownerCoords,
                destinationCoords,
                0.5f,
                cancelToken,
                flags: _pathfinding.GetFlags(blackboard));

            if (path.Result != PathResult.Path || path.Path.Count == 0)
                continue;

            return (true, new Dictionary<string, object>
            {
                { TargetCoordinates, destinationCoords },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }
}

public sealed partial class ADTFindCritEnemyOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public float Range = 5f;

    [DataField]
    public string Key = "Target";

    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var mobState = _entManager.System<MobStateSystem>();
        var transform = _entManager.System<SharedTransformSystem>();
        var containers = _entManager.System<SharedContainerSystem>();
        var ownerPos = transform.GetMapCoordinates(owner).Position;

        EntityUid? best = null;
        var bestDist = float.MaxValue;

        foreach (var hostile in _entManager.System<NpcFactionSystem>().GetNearbyHostiles(owner, Range))
        {
            if (!mobState.IsCritical(hostile) || containers.IsEntityInContainer(hostile))
                continue;

            var dist = Vector2.Distance(ownerPos, transform.GetMapCoordinates(hostile).Position);

            if (dist >= bestDist)
                continue;

            best = hostile;
            bestDist = dist;
        }

        if (best is not { } target)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { Key, target },
            { KeyCoordinates, new EntityCoordinates(target, Vector2.Zero) },
        }));
    }
}

public sealed partial class ADTGroupQuietPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Seconds = 10f;

    [DataField]
    public float Range = 15f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var transform = _entManager.System<SharedTransformSystem>();
        var faction = _entManager.System<NpcFactionSystem>();
        var now = _timing.CurTime;

        foreach (var found in _entManager.System<EntityLookupSystem>()
                     .GetEntitiesInRange<ADTNpcCombatMemoryComponent>(transform.GetMapCoordinates(owner), Range))
        {
            var other = found.Owner;

            if (other != owner && !faction.IsEntityFriendly(owner, other))
                continue;

            if (found.Comp.Spoke && (now - found.Comp.LastSpoke).TotalSeconds < Seconds)
                return false;
        }

        return true;
    }
}

public sealed class ADTNpcRadioSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public const string RadioPrefix = ":я ";
    private const string AnyCall = "*";

    public readonly record struct RadioCall(EntityUid Caller, MapCoordinates Position, string Type, TimeSpan Time);

    private readonly List<RadioCall> _calls = new();
    private readonly Dictionary<(EntityUid, string), TimeSpan> _nextCall = new();

    [ViewVariables]
    public float AnyCallGap = 5f;

    public bool TryCall(EntityUid caller, string type, ProtoId<LocalizedDatasetPrototype> lines, float cooldown, float groupCooldown, float groupRange)
    {
        var now = _timing.CurTime;
        Prune(now);

        if (!_inventory.TryGetSlotEntity(caller, "ears", out _))
            return false;

        if (_nextCall.TryGetValue((caller, type), out var next) && now < next)
            return false;

        if (_nextCall.TryGetValue((caller, AnyCall), out var nextAny) && now < nextAny)
            return false;

        var pos = _transform.GetMapCoordinates(caller);

        foreach (var call in _calls)
        {
            if (call.Type != type || (now - call.Time).TotalSeconds >= groupCooldown)
                continue;

            if (call.Caller == caller)
                return false;

            if (!Exists(call.Caller) || !_faction.IsEntityFriendly(caller, call.Caller))
                continue;

            if (call.Position.MapId == pos.MapId && Vector2.Distance(call.Position.Position, pos.Position) <= groupRange)
                return false;
        }

        if (!_proto.TryIndex(lines, out var set))
            return false;

        _nextCall[(caller, type)] = now + TimeSpan.FromSeconds(cooldown);
        _nextCall[(caller, AnyCall)] = now + TimeSpan.FromSeconds(AnyCallGap);
        _calls.Add(new RadioCall(caller, pos, type, now));

        _chat.TrySendInGameICMessage(caller, RadioPrefix + Loc.GetString(_random.Pick(set)), InGameICChatType.Speak, hideChat: false);
        return true;
    }

    public RadioCall? FindCall(EntityUid listener, IReadOnlyCollection<string> types, float maxAge, float minDistance, float maxDistance)
    {
        var now = _timing.CurTime;
        var pos = _transform.GetMapCoordinates(listener);
        RadioCall? best = null;
        var bestDist = float.MaxValue;

        foreach (var call in _calls)
        {
            if (call.Caller == listener || !types.Contains(call.Type))
                continue;

            if ((now - call.Time).TotalSeconds > maxAge || call.Position.MapId != pos.MapId)
                continue;

            if (!Exists(call.Caller) || !_faction.IsEntityFriendly(listener, call.Caller))
                continue;

            var dist = Vector2.Distance(call.Position.Position, pos.Position);

            if (dist < minDistance || dist > maxDistance || dist >= bestDist)
                continue;

            best = call;
            bestDist = dist;
        }

        return best;
    }

    private void Prune(TimeSpan now)
    {
        _calls.RemoveAll(c => (now - c.Time).TotalSeconds > 120);

        var expired = new List<(EntityUid, string)>();

        foreach (var (key, time) in _nextCall)
        {
            if (time < now)
                expired.Add(key);
        }

        foreach (var key in expired)
            _nextCall.Remove(key);
    }
}

public sealed partial class ADTRadioCallOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    [DataField(required: true)]
    public string Call = string.Empty;

    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> LineSet;

    [DataField]
    public float Chance = 1f;

    [DataField]
    public float Cooldown = 30f;

    [DataField]
    public float GroupCooldown = 20f;

    [DataField]
    public float GroupRange = 30f;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_random.Prob(Chance))
            _entManager.System<ADTNpcRadioSystem>().TryCall(owner, Call, LineSet, Cooldown, GroupCooldown, GroupRange);

        return HTNOperatorStatus.Finished;
    }
}

public sealed partial class ADTRadioCallPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField(required: true)]
    public List<string> Types = new();

    [DataField]
    public float MaxAge = 30f;

    [DataField]
    public float MinDistance = 6f;

    [DataField]
    public float MaxDistance = 40f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _entManager.System<ADTNpcRadioSystem>().FindCall(owner, Types, MaxAge, MinDistance, MaxDistance) != null;
    }
}

public sealed partial class ADTPickRadioCallOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField(required: true)]
    public List<string> Types = new();

    [DataField]
    public float MaxAge = 30f;

    [DataField]
    public float MinDistance = 6f;

    [DataField]
    public float MaxDistance = 40f;

    [DataField]
    public string TargetCoordinates = "TargetCoordinates";

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_entManager.System<ADTNpcRadioSystem>().FindCall(owner, Types, MaxAge, MinDistance, MaxDistance) is not { } call)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        var coords = _entManager.System<SharedTransformSystem>().ToCoordinates(call.Position);

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, new Dictionary<string, object>
        {
            { TargetCoordinates, coords },
        }));
    }
}

public sealed class ADTRandomHairSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTRandomHairComponent, MapInitEvent>(OnMapInit, after: new[] { typeof(RandomHumanoidAppearanceSystem) });
    }

    private void OnMapInit(EntityUid uid, ADTRandomHairComponent component, MapInitEvent args)
    {
        if (component.Styles.Count == 0 || component.Colors.Count == 0)
            return;

        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        var style = _random.Pick(component.Styles);
        var color = _random.Pick(component.Colors);

        foreach (var layers in profile.Appearance.Markings.Values)
        {
            if (component.ClearOtherMarkings)
            {
                foreach (var layer in layers.Keys.ToList())
                {
                    if (layer is HumanoidVisualLayers.Hair or HumanoidVisualLayers.FacialHair)
                        continue;

                    layers[layer] = new List<Marking>();
                }
            }

            if (layers.ContainsKey(HumanoidVisualLayers.Hair))
                layers[HumanoidVisualLayers.Hair] = new List<Marking> { new(style, new[] { color }) };

            if (layers.TryGetValue(HumanoidVisualLayers.FacialHair, out var facial))
            {
                layers[HumanoidVisualLayers.FacialHair] = facial
                    .Select(marking => new Marking(marking.MarkingId, Enumerable.Repeat(color, Math.Max(1, marking.MarkingColors.Count))))
                    .ToList();
            }
        }

        profile.Appearance.HairColor = new List<Color> { color };

        if (_proto.TryIndex<SpeciesPrototype>(humanoid.Species, out var species) &&
            _proto.TryIndex(species.SkinColoration, out var coloration) &&
            coloration.Strategy.InputType == SkinColorationStrategyInput.Unary)
        {
            profile.Appearance.SkinColor = coloration.Strategy.FromUnary(_random.NextFloat(component.MinSkinTone, component.MaxSkinTone));
        }

        _visualBody.ApplyProfileTo(uid, profile);
        _humanoidProfile.ApplyProfileTo(uid, profile);
    }
}

public sealed partial class ADTTargetAlivePrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public string TargetKey = "Target";

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return false;

        return _entManager.System<MobStateSystem>().IsAlive(target);
    }
}

public sealed partial class ADTExecuteOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string StartedKey = "ADTExecuteStarted";
    private const string TimerKey = "ADTExecuteTimer";

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 1.6f;

    [DataField]
    public float GunExecutionTime = 4f;

    [DataField]
    public float Timeout = 12f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(StartedKey, false);
        blackboard.SetValue(TimerKey, 0f);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
        blackboard.SetValue(TimerKey, timer);

        if (blackboard.TryGetValue<bool>(StartedKey, out var started, _entManager) && started)
        {
            if (_entManager.HasComponent<ActiveDoAfterComponent>(owner))
                return timer < Timeout ? HTNOperatorStatus.Continuing : HTNOperatorStatus.Failed;

            return HTNOperatorStatus.Finished;
        }

        if (!blackboard.TryGetValue<EntityUid>(Key, out var victim, _entManager) || _entManager.Deleted(victim))
            return HTNOperatorStatus.Failed;

        var transform = _entManager.System<SharedTransformSystem>();

        if (Vector2.Distance(transform.GetMapCoordinates(owner).Position, transform.GetMapCoordinates(victim).Position) > MaxDistance)
            return HTNOperatorStatus.Failed;

        if (_entManager.System<SharedHandsSystem>().GetActiveItem(owner) is not { } weapon)
            return HTNOperatorStatus.Failed;

        if (!_entManager.System<SharedExecutionSystem>().CanBeExecuted(victim, owner))
            return HTNOperatorStatus.Failed;

        float duration;
        LocId message;

        if (_entManager.HasComponent<GunComponent>(weapon))
        {
            ADTNpcWeaponHelper.GetAmmo(_entManager, weapon, out var count, out _);

            if (count <= 0)
                return HTNOperatorStatus.Failed;

            duration = GunExecutionTime;
            message = "execution-popup-gun-initial-external";
        }
        else if (_entManager.TryGetComponent(weapon, out ExecutionComponent? execution))
        {
            duration = execution.DoAfterDuration;
            message = execution.ExternalMeleeExecutionMessage;
        }
        else
        {
            return HTNOperatorStatus.Failed;
        }

        _entManager.System<SharedCombatModeSystem>().SetInCombatMode(owner, false);

        var args = new DoAfterArgs(_entManager, owner, TimeSpan.FromSeconds(duration), new ExecutionDoAfterEvent(), weapon, target: victim, used: weapon)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_entManager.System<SharedDoAfterSystem>().TryStartDoAfter(args))
            return HTNOperatorStatus.Failed;

        _entManager.System<SharedPopupSystem>().PopupEntity(
            Loc.GetString(message,
                ("attacker", Identity.Entity(owner, _entManager)),
                ("victim", Identity.Entity(victim, _entManager)),
                ("weapon", weapon)),
            owner,
            PopupType.MediumCaution);

        blackboard.SetValue(StartedKey, true);
        blackboard.SetValue(TimerKey, 0f);
        return HTNOperatorStatus.Continuing;
    }
}
