using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class MeleeChargeProjectileEffect : TrophyEffect
{
    [DataField]
    public DamageSpecifier BonusDamage = new();

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-melee-charge"));
    }

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

            var comp = entManager.EnsureComponent<MeleeChargeActiveEffectComponent>(holder.Owner);

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
        if (!entManager.TryGetComponent<MeleeChargeActiveEffectComponent>(holder.Owner, out var magmaComp))
            return;

        if (!entManager.HasComponent<MobStateComponent>(args.Target))
            return;

        var damageableSystem = entManager.System<DamageableSystem>();

        damageableSystem.TryChangeDamage(args.Target, BonusDamage, origin: holder);
        entManager.RemoveComponentDeferred<MeleeChargeActiveEffectComponent>(holder.Owner);
    }
}
