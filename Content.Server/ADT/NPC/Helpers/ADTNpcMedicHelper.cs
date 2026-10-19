using System.Linq;
using Content.Server.NPC;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.Helpers;

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
        foreach (var item in ADTNpcItemHelper.ReserveItems(ent, medic))
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

        foreach (var (_, held) in ADTNpcItemHelper.HeldItems(ent, medic))
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
