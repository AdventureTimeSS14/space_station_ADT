using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Marker;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class DamageAmplifyOnHitEffect : TrophyEffect
{
    [DataField]
    public float DamageMultiplier = 1.1f;

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(2);

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-damage-amplify",
            ("duration", EffectDuration.TotalSeconds.ToString("F1")),
            ("multiplier", ((DamageMultiplier - 1f) * 100f).ToString("F0"))));
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

            var active = new DamageAmplifyActiveEffectComponent
            {
                DamageMult = DamageMultiplier,
                ExpireTime = gameTiming.CurTime + EffectDuration,
            };

            if (!entManager.HasComponent<DamageAmplifyActiveEffectComponent>(target))
                entManager.AddComponent(target, active);
        }
    }
}
