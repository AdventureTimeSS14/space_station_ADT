using System.Linq;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class TrophyEffectsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageAmplifyActiveEffectComponent, DamageModifyEvent>(OnDamageAmplifyModify);
        SubscribeLocalEvent<SlowDebuffMarkerComponent, MeleeHitEvent>(OnSlowDebuffMeleeHit);
        SubscribeLocalEvent<DamageShieldActiveEffectComponent, DamageModifyEvent>(OnDamageShieldModify);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var amplifyQuery = EntityQueryEnumerator<DamageAmplifyActiveEffectComponent>();
        while (amplifyQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireTime != TimeSpan.Zero && comp.ExpireTime < now)
                RemCompDeferred<DamageAmplifyActiveEffectComponent>(uid);
        }

        var shieldQuery = EntityQueryEnumerator<DamageShieldActiveEffectComponent>();
        while (shieldQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireTime != TimeSpan.Zero && comp.ExpireTime < now)
                RemCompDeferred<DamageShieldActiveEffectComponent>(uid);
        }

        var debuffQuery = EntityQueryEnumerator<SlowDebuffMarkerComponent>();
        while (debuffQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireTime != TimeSpan.Zero && comp.ExpireTime < now)
                RemCompDeferred<SlowDebuffMarkerComponent>(uid);
        }
    }

    private void OnDamageAmplifyModify(Entity<DamageAmplifyActiveEffectComponent> ent, ref DamageModifyEvent args)
    {
        var keys = args.Damage.DamageDict.Keys.ToList();
        foreach (var type in keys)
        {
            args.Damage.DamageDict[type] *= ent.Comp.DamageMult;
        }
    }

    private void OnSlowDebuffMeleeHit(Entity<SlowDebuffMarkerComponent> ent, ref MeleeHitEvent args)
    {
        var keys = args.BaseDamage.DamageDict.Keys.ToList();
        foreach (var type in keys)
        {
            args.BaseDamage.DamageDict[type] *= ent.Comp.DamageMultiplier;
        }
    }

    private void OnDamageShieldModify(Entity<DamageShieldActiveEffectComponent> ent, ref DamageModifyEvent args)
    {
        var keys = args.Damage.DamageDict.Keys.ToList();
        foreach (var type in keys)
        {
            args.Damage.DamageDict[type] *= ent.Comp.DamageReductionMultiplier;
        }
    }
}
