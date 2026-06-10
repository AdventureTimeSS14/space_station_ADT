using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Crushers.Effects;

public sealed partial class DemonClawsEffect : TrophyEffect
{
    [DataField]
    public float BonusDamage = 2f;

    [DataField]
    public float HealOnHit = 1f;

    [DataField]
    public float MarkDetonationMultiplier = 5f;

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
            totalWeaponDamage += (float) val;

        if (totalWeaponDamage <= 0f)
            return;

        var bonusSpecifier = new DamageSpecifier();
        foreach (var (type, val) in weaponDamage.DamageDict)
            bonusSpecifier.DamageDict[type] = (float) val / totalWeaponDamage * BonusDamage;

        var healSpecifier = new DamageSpecifier();
        foreach (var (type, val) in weaponDamage.DamageDict)
            healSpecifier.DamageDict[type] = -(float) val / totalWeaponDamage * HealOnHit;

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
