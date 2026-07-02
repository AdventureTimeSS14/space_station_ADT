using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class LifestealOnHitEffect : TrophyEffect
{
    [DataField]
    public float BonusDamage = 2f;

    [DataField]
    public float HealOnHit = 1f;

    [DataField]
    public float MarkDetonationMultiplier = 5f;

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-lifesteal",
            ("damage", BonusDamage.ToString("F1")),
            ("heal", HealOnHit.ToString("F1")),
            ("multiplier", ((MarkDetonationMultiplier - 1f) * 100f).ToString("F0"))));
    }

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (!entManager.TryGetComponent<MeleeWeaponComponent>(args.Weapon, out var melee))
            return;

        var weaponDamage = melee.Damage;

        var totalWeaponDamage = 0f;
        foreach (var (_, val) in weaponDamage.DamageDict)
        {
            if (val > 0)
                totalWeaponDamage += (float) val;
        }

        if (totalWeaponDamage <= 0f)
            return;

        var bonusSpecifier = new DamageSpecifier();
        var healSpecifier = new DamageSpecifier();

        foreach (var (type, val) in weaponDamage.DamageDict)
        {
            var floatVal = (float) val;
            if (floatVal <= 0f)
                continue;

            var ratio = floatVal / totalWeaponDamage;
            bonusSpecifier.DamageDict[type] = ratio * BonusDamage;
            healSpecifier.DamageDict[type] = -ratio * HealOnHit;
        }

        var damageable = entManager.System<DamageableSystem>();
        var detonated = false;

        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner || target == args.User)
                continue;

            damageable.TryChangeDamage(target, bonusSpecifier, origin: holder);

            if (entManager.HasComponent<DamageMarkerComponent>(target))
                detonated = true;
        }

        if (detonated)
            healSpecifier *= MarkDetonationMultiplier;

        damageable.TryChangeDamage(args.User, healSpecifier, true);
    }
}
