using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Marker;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class HealthScalingDamageEffect : TrophyEffect
{
    [DataField]
    public float Multiplier = 0.1f;

    [DataField]
    public float ScalingFactor = 2.0f;

    [DataField]
    public DamageSpecifier BonusDamage = new DamageSpecifier()
    {
        DamageDict = { { "Blunt", 0 } }
    };

    public override FormattedMessage GetDescription()
    {
        var scalingPercent = (Multiplier * ScalingFactor * 100f).ToString("F0");
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-health-scaling",
            ("percent", scalingPercent)));
    }

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        var damageableSystem = entManager.System<DamageableSystem>();
        if (!entManager.TryGetComponent<DamageableComponent>(args.User, out var damageable))
            return;

        var totalDamage = damageableSystem.GetTotalDamage((args.User, damageable));
        if (totalDamage <= FixedPoint2.Zero)
            return;

        var scaledDamage = new DamageSpecifier();
        foreach (var type in BonusDamage.DamageDict.Keys)
            scaledDamage.DamageDict[type] = totalDamage * (FixedPoint2)(Multiplier * ScalingFactor);

        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner || target == args.User)
                continue;

            if (!entManager.HasComponent<DamageMarkerComponent>(target))
                continue;

            damageableSystem.TryChangeDamage(target, scaledDamage, origin: holder);
        }
    }
}
