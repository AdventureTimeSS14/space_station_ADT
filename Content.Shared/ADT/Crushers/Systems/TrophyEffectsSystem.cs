using System.Linq;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;
using Robust.Shared.Log;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class TrophyEffectsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PoisonFangActiveComponent, DamageModifyEvent>(OnPoisonFangDamageModify);
        SubscribeLocalEvent<FrostGlandMarkerComponent, MeleeHitEvent>(OnFrostGlandMeleeHit);
        SubscribeLocalEvent<BloodDrunkMinerEyeActiveComponent, DamageModifyEvent>(OnBloodDrunkDamageModify);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var poisonQuery = EntityQueryEnumerator<PoisonFangActiveComponent>();
        while (poisonQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireTime != TimeSpan.Zero && comp.ExpireTime < now)
                RemCompDeferred<PoisonFangActiveComponent>(uid);
        }

        var bloodDrunkQuery = EntityQueryEnumerator<BloodDrunkMinerEyeActiveComponent>();
        while (bloodDrunkQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireTime != TimeSpan.Zero && comp.ExpireTime < now)
                RemCompDeferred<BloodDrunkMinerEyeActiveComponent>(uid);
        }

        var frostGlandQuery = EntityQueryEnumerator<FrostGlandMarkerComponent>();
        while (frostGlandQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpireTime != TimeSpan.Zero && comp.ExpireTime < now)
                RemCompDeferred<FrostGlandMarkerComponent>(uid);
        }
    }

    private void OnPoisonFangDamageModify(Entity<PoisonFangActiveComponent> ent, ref DamageModifyEvent args)
    {
        var keys = args.Damage.DamageDict.Keys.ToList();
        foreach (var type in keys)
        {
            var before = args.Damage.DamageDict[type];
            args.Damage.DamageDict[type] *= ent.Comp.DamageMult;
        }
    }

    private void OnFrostGlandMeleeHit(Entity<FrostGlandMarkerComponent> ent, ref MeleeHitEvent args)
    {
        var keys = args.BaseDamage.DamageDict.Keys.ToList();
        foreach (var type in keys)
        {
            var before = args.BaseDamage.DamageDict[type];
            args.BaseDamage.DamageDict[type] *= ent.Comp.DamageMultiplier;
        }
    }

    private void OnBloodDrunkDamageModify(Entity<BloodDrunkMinerEyeActiveComponent> ent, ref DamageModifyEvent args)
    {
        var keys = args.Damage.DamageDict.Keys.ToList();
        foreach (var type in keys)
        {
            var before = args.Damage.DamageDict[type];
            args.Damage.DamageDict[type] *= ent.Comp.DamageReductionMultiplier;
        }
    }
}
