using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;

namespace Content.Shared.ADT.ArmorPenetration;

public sealed class ArmorPenetrationSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorPenetrationComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ArmorPenetrationComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnMeleeHit(Entity<ArmorPenetrationComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var target = args.HitEntities.FirstOrDefault(uid => HasComp<MobStateComponent>(uid));
        if (target == default)
            return;

        if (TryGetCompensator(ent.Comp.Penetration, target, out var compensator))
            args.ModifiersList.Add(compensator);
    }

    private void OnProjectileHit(Entity<ArmorPenetrationComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (TryGetCompensator(ent.Comp.Penetration, args.Target, out var compensator))
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, compensator);
    }

    public DamageSpecifier ApplyPenetration(EntityUid target, DamageSpecifier damage, float penetration)
    {
        if (!HasComp<MobStateComponent>(target) || !TryGetCompensator(penetration, target, out var compensator))
            return damage;

        return DamageSpecifier.ApplyModifierSet(damage, compensator);
    }

    private bool TryGetCompensator(float rawPenetration, EntityUid target, [NotNullWhen(true)] out DamageModifierSet? compensator)
    {
        compensator = null;

        var penetration = Math.Clamp(rawPenetration, 0f, 1f);
        if (penetration <= 0f)
            return false;

        if (!TryComp<InventoryComponent>(target, out var inventory))
            return false;

        var query = new CoefficientQueryEvent(~SlotFlags.POCKET);
        _inventory.RelayEvent((target, inventory), query);

        if (query.DamageModifiers.Coefficients.Count == 0)
            return false;

        var result = new DamageModifierSet();
        foreach (var (damageType, c) in query.DamageModifiers.Coefficients)
        {
            if (c <= 0f || c >= 1f)
                continue;

            var multiplier = (c + penetration * (1f - c)) / c;
            result.Coefficients[damageType] = multiplier;
        }

        if (result.Coefficients.Count == 0)
            return false;

        compensator = result;
        return true;
    }
}
