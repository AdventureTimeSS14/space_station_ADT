using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;

namespace Content.Shared.ADT.ArmorPenetration;

public sealed partial class ArmorPenetrationSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

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

        if (!TryGetArmorCompensator(target, ent.Comp.Penetration, out var compensator))
            return;

        args.ModifiersList.Add(compensator);
    }

    private void OnProjectileHit(Entity<ArmorPenetrationComponent> ent, ref ProjectileHitEvent args)
    {
        if (!TryGetArmorCompensator(args.Target, ent.Comp.Penetration, out var compensator))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, compensator);
    }

    private bool TryGetArmorCompensator(EntityUid target, float penetration, out DamageModifierSet compensator)
    {
        compensator = new();

        penetration = Math.Clamp(penetration, 0f, 1f);
        if (penetration <= 0f)
            return false;

        if (!TryComp<InventoryComponent>(target, out var inventory))
            return false;

        var query = new CoefficientQueryEvent(~SlotFlags.POCKET);
        _inventory.RelayEvent((target, inventory), query);

        if (query.DamageModifiers.Coefficients.Count == 0)
            return false;

        foreach (var (damageType, c) in query.DamageModifiers.Coefficients)
        {
            if (c <= 0f || c >= 1f)
                continue;

            var multiplier = (c + penetration * (1f - c)) / c;
            compensator.Coefficients[damageType] = multiplier;
        }

        return compensator.Coefficients.Count > 0;
    }
}
