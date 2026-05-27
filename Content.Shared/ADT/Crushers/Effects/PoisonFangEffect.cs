using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Marker;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Effects;

public sealed partial class PoisonFangEffect : TrophyEffect
{
    [DataField]
    public float DamageMultiplier = 1.1f;

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(2);

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        var gameTiming = IoCManager.Resolve<IGameTiming>();

        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner)
                continue;

            if (!entManager.HasComponent<DamageMarkerComponent>(target))
                continue;

            var active = new PoisonFangActiveComponent
            {
                DamageMult = DamageMultiplier,
                ExpireTime = gameTiming.CurTime + EffectDuration,
            };

            if (!entManager.HasComponent<PoisonFangActiveComponent>(target))
                entManager.AddComponent(target, active);
        }
    }
}
