using Content.Server.NPC;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Stacks;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcGearSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    public const string IgnoredHealItemsKey = "ADTIgnoredHealItems";
    public const string IgnoredWeaponsKey = "ADTIgnoredWeapons";
    public const string MedipenTimesKey = "ADTMedipenTimes";

    public const string MedActionKey = "ADTMedAction";
    public const string MedActionHeal = "Heal";
    public const string MedActionMedipen = "Medipen";
    public const string MedActionDefib = "Defib";

    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly EntProtoId[] Medipens = { "CombatMedipen", "EmergencyMedipen" };
    private static readonly TimeSpan MedipenMemory = TimeSpan.FromMinutes(5);

    public float DamageFraction(EntityUid uid)
    {
        if (!_thresholds.TryGetThresholdForState(uid, MobState.SoftCritical, out var amount) &&
            !_thresholds.TryGetThresholdForState(uid, MobState.Critical, out amount))
        {
            return 0f;
        }

        return (float) (_damageable.GetTotalDamage(uid) / amount.Value);
    }

    public bool IsBleeding(EntityUid uid)
    {
        return TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.BleedAmount > 0f;
    }

    private int HealScore(EntityUid item, bool bleeding, Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>? damage)
    {
        if (!TryComp<HealingComponent>(item, out var healing))
            return 0;

        if (TryComp<StackComponent>(item, out var stack) && stack.Count < 1)
            return 0;

        var healsBlunt = healing.Damage.DamageDict.TryGetValue(BluntDamage, out var blunt) && blunt < 0;
        var stopsBleeding = healing.BloodlossModifier < 0f;
        var bandage = stopsBleeding && !healsBlunt;

        if (bleeding && stopsBleeding)
            return bandage ? 1000 : 900;

        if (bandage || damage == null)
            return 0;

        var useful = 0f;

        foreach (var (type, amount) in healing.Damage.DamageDict)
        {
            if (amount < 0 && damage.TryGetValue(type, out var have) && have > 0)
                useful += -(float) amount;
        }

        return (int) useful;
    }

    public int HealScore(EntityUid patient, EntityUid item)
    {
        return HealScore(item, IsBleeding(patient), PatientDamage(patient));
    }

    private Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>? PatientDamage(EntityUid patient)
    {
        return TryComp<DamageableComponent>(patient, out var damageable) ? damageable.Damage.DamageDict : null;
    }

    public EntityUid? FindHealItem(EntityUid owner, HashSet<EntityUid>? ignored, out int bestScore)
    {
        return FindHealItemFor(owner, owner, ignored, out bestScore);
    }

    public EntityUid? FindHealItemFor(EntityUid medic, EntityUid patient, HashSet<EntityUid>? ignored, out int bestScore)
    {
        EntityUid? best = null;
        bestScore = 0;

        var bleeding = IsBleeding(patient);
        var damage = PatientDamage(patient);

        foreach (var entry in GetReserve(medic))
        {
            var item = entry.Item;

            if (ignored != null && ignored.Contains(item))
                continue;

            var score = HealScore(item, bleeding, damage);

            if (score <= bestScore || !IsCarried(medic, item))
                continue;

            best = item;
            bestScore = score;
        }

        return best;
    }

    public static HashSet<EntityUid>? GetIgnored(NPCBlackboard blackboard, string key, IEntityManager entMan)
    {
        return blackboard.TryGetValue<HashSet<EntityUid>>(key, out var stored, entMan) ? stored : null;
    }

    public void Ignore(NPCBlackboard blackboard, string key, EntityUid item)
    {
        if (GetIgnored(blackboard, key, EntityManager) is not { } ignored)
        {
            ignored = new HashSet<EntityUid>();
            blackboard.SetValue(key, ignored);
        }

        ignored.RemoveWhere(uid => TerminatingOrDeleted(uid));
        ignored.Add(item);
    }

    public bool IsMedipen(EntityUid item)
    {
        return MetaData(item).EntityPrototype?.ID is { } id && Array.IndexOf(Medipens, (EntProtoId) id) >= 0;
    }

    public EntityUid? FindMedipen(EntityUid medic)
    {
        foreach (var entry in GetReserve(medic))
        {
            if (IsMedipen(entry.Item) && IsCarried(medic, entry.Item))
                return entry.Item;
        }

        return null;
    }

    public FixedPoint2 SolutionVolume(EntityUid item)
    {
        var total = FixedPoint2.Zero;

        foreach (var (_, solution) in _solutions.EnumerateSolutions(item))
        {
            total += solution.Comp.Solution.Volume;
        }

        return total;
    }

    public EntityUid? FindDefib(EntityUid medic)
    {
        if (_inventory.TryGetSlotEntity(medic, "suitstorage", out var worn) && HasComp<DefibrillatorComponent>(worn.Value))
            return _powerCell.HasActivatableCharge(worn.Value) ? worn : null;

        foreach (var entry in GetHeld(medic))
        {
            if (HasComp<DefibrillatorComponent>(entry.Item))
                return _powerCell.HasActivatableCharge(entry.Item) ? entry.Item : null;
        }

        return null;
    }

    public bool CanBeRevived(EntityUid patient)
    {
        if (HasComp<UnrevivableComponent>(patient))
            return false;

        return !_rotting.IsRotten(patient);
    }

    public bool RevivableByZap(EntityUid patient, EntityUid defib)
    {
        if (!_thresholds.TryGetThresholdForState(patient, MobState.Dead, out var threshold))
            return false;

        if (!TryComp<DefibrillatorComponent>(defib, out var defibComp) || PatientDamage(patient) is not { } damage)
            return false;

        var total = (float) _damageable.GetTotalDamage(patient);

        foreach (var (type, amount) in defibComp.ZapHeal.DamageDict)
        {
            if (amount >= 0 || !damage.TryGetValue(type, out var have) || have <= 0)
                continue;

            total -= MathF.Min((float) have, -(float) amount);
        }

        return total < (float) threshold.Value - 1f;
    }

    public bool MedipenRecentlyUsed(NPCBlackboard blackboard, EntityUid patient, float cooldown)
    {
        if (!blackboard.TryGetValue<Dictionary<EntityUid, TimeSpan>>(MedipenTimesKey, out var times, EntityManager))
            return false;

        return times.TryGetValue(patient, out var at) && (_timing.CurTime - at).TotalSeconds < cooldown;
    }

    public void MarkMedipen(NPCBlackboard blackboard, EntityUid patient)
    {
        var now = _timing.CurTime;

        if (!blackboard.TryGetValue<Dictionary<EntityUid, TimeSpan>>(MedipenTimesKey, out var times, EntityManager))
        {
            times = new Dictionary<EntityUid, TimeSpan>();
            blackboard.SetValue(MedipenTimesKey, times);
        }

        foreach (var (uid, at) in times)
        {
            if (now - at > MedipenMemory || TerminatingOrDeleted(uid))
                times.Remove(uid);
        }

        times[patient] = now;
    }
}
