using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Marker;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Components;

namespace Content.Shared.ADT.Crushers.Effects;

public sealed partial class BloodDrunkMinerEyeEffect : TrophyEffect
{
    [DataField]
    public TimeSpan ProtectionDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float DamageReductionMultiplier = 0.1f; // 90% снижение

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
            var active = new BloodDrunkMinerEyeActiveComponent
            {
                DamageReductionMultiplier = DamageReductionMultiplier,
                ExpireTime = gameTiming.CurTime + ProtectionDuration
            };

            if (!entManager.HasComponent<BloodDrunkMinerEyeActiveComponent>(args.User))
            {
                entManager.AddComponent(args.User, active);
                statusEffects.TryAddStatusEffect<IgnoreSlowOnDamageComponent>(args.User, "Adrenaline", ProtectionDuration, true, null);
            }
            break;
        }
    }
}