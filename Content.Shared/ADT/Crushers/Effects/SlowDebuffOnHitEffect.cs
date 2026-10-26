using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class SlowDebuffOnHitEffect : TrophyEffect
{
    [DataField]
    public float DamageMultiplier = 0.9f;

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(2);

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-slow-debuff",
            ("duration", EffectDuration.TotalSeconds.ToString("F1")),
            ("reduction", ((1f - DamageMultiplier) * 100f).ToString("F0"))));
    }

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

            if (entManager.TryGetComponent<SlowDebuffMarkerComponent>(target, out var active))
            {
                active.ExpireTime = gameTiming.CurTime + EffectDuration;
                entManager.Dirty(target, active);
                continue;
            }

            var newMarker = new SlowDebuffMarkerComponent
            {
                DamageMultiplier = DamageMultiplier,
                ExpireTime = EffectDuration + gameTiming.CurTime,
                Source = holder.Owner,
            };

            entManager.AddComponent(target, newMarker);
        }
    }
}
