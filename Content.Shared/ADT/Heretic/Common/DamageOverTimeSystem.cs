using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Clothing.Systems

public sealed class DamageOverTimeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageSys = default!;

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<DamageOverTimeComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (currentTime < component.NextTickTime)
                continue;
            component.NextTickTime = currentTime + component.Interval;
            _damageSys.TryChangeDamage(uid,
                component.Damage * component.Multiplier,
                ignoreResistances: component.IgnoreResistances);
            component.Multiplier += component.MultiplierIncrease;
            Dirty(uid, component);
        }
    }
}
