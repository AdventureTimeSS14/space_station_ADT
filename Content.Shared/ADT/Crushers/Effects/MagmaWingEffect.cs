using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Crushers.Effects;

public sealed partial class MagmaWingEffect : TrophyEffect
{
    [DataField]
    public DamageSpecifier BonusDamage = new DamageSpecifier();

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

            var comp = entManager.EnsureComponent<MagmaWingActiveComponent>(holder.Owner);

            comp.BonusDamage = new DamageSpecifier(BonusDamage);
            entManager.Dirty(holder.Owner, comp);
            break;
        }
    }

    public override void OnProjectileHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref ProjectileHitEvent args)
    {
        if (!entManager.TryGetComponent<MagmaWingActiveComponent>(holder.Owner, out var magmaComp))
            return;

        if (!entManager.HasComponent<MobStateComponent>(args.Target))
            return;

        var damageableSystem = entManager.System<DamageableSystem>();

        damageableSystem.TryChangeDamage(args.Target, BonusDamage, origin: holder);
        entManager.RemoveComponentDeferred<MagmaWingActiveComponent>(holder.Owner);
    }
}
