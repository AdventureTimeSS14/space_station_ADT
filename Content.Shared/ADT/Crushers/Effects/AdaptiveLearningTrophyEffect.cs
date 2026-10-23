using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class AdaptiveLearningTrophyEffect : TrophyEffect
{
    [DataField]
    public float BonusPerHit = 2f;

    [DataField]
    public float MaxBonus = 20f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    [DataField]
    public string DamageType = "Blunt";

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-adaptive-learning",
            ("bonus", BonusPerHit),
            ("max", MaxBonus)));
    }

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        var timing = IoCManager.Resolve<IGameTiming>();

        if (!timing.IsFirstTimePredicted)
            return;

        var now = timing.CurTime;

        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner || target == args.User || !entManager.HasComponent<MobStateComponent>(target))
                continue;

            var learning = entManager.EnsureComponent<AdaptiveLearningActiveEffectComponent>(target);

            if (now >= learning.ExpireTime)
            {
                learning.Bonus = 0f;
                learning.ExpireTime = now + Duration;
            }

            if (learning.Bonus > 0f)
            {
                var bonus = new DamageSpecifier();
                bonus.DamageDict.Add(DamageType, learning.Bonus);
                args.BonusDamage += bonus;
            }

            learning.Bonus = MathF.Min(learning.Bonus + BonusPerHit, MaxBonus);
            break;
        }
    }
}
