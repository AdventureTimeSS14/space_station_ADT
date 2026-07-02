using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Marker;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class DamageShieldOnHitEffect : TrophyEffect
{
    [DataField]
    public TimeSpan ProtectionDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float DamageReductionMultiplier = 0.1f;

    [DataField]
    public string StatusEffect = "Adrenaline";

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-damage-shield",
            ("duration", ProtectionDuration.TotalSeconds.ToString("F1")),
            ("reduction", ((1f - DamageReductionMultiplier) * 100f).ToString("F0"))));
    }

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner || target == args.User)
                continue;

            if (!entManager.HasComponent<DamageMarkerComponent>(target))
                continue;

            var gameTiming = IoCManager.Resolve<IGameTiming>();
            var statusEffects = entManager.System<StatusEffectsSystem>();
            var active = new DamageShieldActiveEffectComponent
            {
                DamageReductionMultiplier = DamageReductionMultiplier,
                ExpireTime = gameTiming.CurTime + ProtectionDuration
            };

            if (!entManager.HasComponent<DamageShieldActiveEffectComponent>(args.User))
            {
                entManager.AddComponent(args.User, active);
                statusEffects.TryAddStatusEffect<IgnoreSlowOnDamageComponent>(args.User, StatusEffect, ProtectionDuration, true, null);
            }
            break;
        }
    }
}
