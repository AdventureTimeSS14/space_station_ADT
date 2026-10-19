using System.Linq;
using Content.Server.NPC;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stacks;

namespace Content.Server.ADT.NPC.Helpers;

public static class ADTNpcHealHelper
{
    public const string IgnoredKey = "ADTIgnoredHealItems";

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

        foreach (var item in ADTNpcItemHelper.ReserveItems(ent, medic))
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
        return blackboard.TryGetValue<HashSet<EntityUid>>(IgnoredKey, out var stored, ent)
            ? stored
            : null;
    }
}
