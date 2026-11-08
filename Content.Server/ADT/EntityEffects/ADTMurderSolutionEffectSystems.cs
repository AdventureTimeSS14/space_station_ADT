using Content.Shared.ADT.Cloning;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ADTCureCloneCellularDecayEntityEffectSystem : EntityEffectSystem<ADTCloneCellularDecayComponent, ADTCureCloneCellularDecay>
{
    protected override void Effect(Entity<ADTCloneCellularDecayComponent> entity, ref EntityEffectEvent<ADTCureCloneCellularDecay> args)
    {
        RemCompDeferred<ADTCloneCellularDecayComponent>(entity);
    }
}

public sealed partial class ADTMurderSolutionBurstEntityEffectSystem : EntityEffectSystem<DamageableComponent, ADTMurderSolutionBurst>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<ADTMurderSolutionBurst> args)
    {
        var exposed = EnsureComp<ADTMurderSolutionExposedComponent>(entity);
        if (_timing.CurTime < exposed.NextBurst)
            return;

        exposed.NextBurst = _timing.CurTime + args.Effect.Cooldown;
        _damageable.TryChangeDamage(entity.AsNullable(), args.Effect.Damage);
    }
}
