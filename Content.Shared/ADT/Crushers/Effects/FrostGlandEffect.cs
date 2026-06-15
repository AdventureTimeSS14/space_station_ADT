using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class FrostGlandEffect : TrophyEffect
{
    [DataField]
    public float DamageMultiplier = 0.9f;

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(2);

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner)
                continue;

            if (!entManager.HasComponent<DamageMarkerComponent>(target))
                continue;

            var gameTiming = IoCManager.Resolve<IGameTiming>();
            var marker = new FrostGlandMarkerComponent
            {
                DamageMultiplier = DamageMultiplier,
                ExpireTime = EffectDuration + gameTiming.CurTime
            };

            if (!entManager.HasComponent<FrostGlandMarkerComponent>(target))
                entManager.AddComponent(target, marker);
        }
    }
}
