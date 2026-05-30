using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Crushers.Effects;

public sealed partial class DemonClawsEffect : TrophyEffect
{
    [DataField]
    public DamageSpecifier HealOnHit = new()
    {
        DamageDict = { { "Blunt", -1 } },
    };

    [DataField]
    public DamageSpecifier BonusDamage = new()
    {
        DamageDict = { { "Slash", 2 } },
    };

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

        var damageable = entManager.System<DamageableSystem>();

        var detonated = false;

        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner || target == args.User)
                continue;

            damageable.TryChangeDamage(target, BonusDamage, origin: holder);

            if (entManager.HasComponent<DamageMarkerComponent>(target))
                detonated = true;
        }

        var heal = HealOnHit;
        if (detonated)
            heal = HealOnHit * MarkDetonationMultiplier;

        damageable.TryChangeDamage(args.User, heal, true);
    }
}
