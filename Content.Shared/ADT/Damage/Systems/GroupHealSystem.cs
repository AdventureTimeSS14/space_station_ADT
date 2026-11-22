using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.ADT.Damage.Systems;

public sealed class GroupHealSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    public void Heal(EntityUid target, DamageSpecifier? heal, GroupHealSpecifier? groups, float multiplier = 1f, bool weighted = true, EntityUid? origin = null)
    {
        if (multiplier <= 0f)
            return;

        if (heal != null && !heal.Empty)
            _damageable.TryChangeDamage(target, heal * multiplier, true, false, origin: origin);

        if (groups == null)
            return;

        foreach (var (group, amount) in groups.GroupHealDict)
        {
            if (amount >= 0)
                continue;

            if (weighted)
                _damageable.HealDistributed(target, amount * multiplier, group, origin);
            else
                _damageable.HealEvenly(target, amount * multiplier, group, origin);
        }
    }
}
