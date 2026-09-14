using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Mobs.Components;

namespace Content.Shared.ADT.ArmorPenetration;

public sealed class ArmorPenetrationSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorPenetrationComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ArmorPenetrationComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var penetration = Math.Clamp(ent.Comp.Penetration, 0f, 1f);
        if (penetration <= 0f)
            return;


        var target = args.HitEntities.FirstOrDefault(uid => HasComp<MobStateComponent>(uid));
        if (target == default)
            return;

        if (!TryComp<InventoryComponent>(target, out var inventory))
            return;

        var query = new CoefficientQueryEvent(~SlotFlags.POCKET);
        _inventory.RelayEvent((target, inventory), query);

        if (query.DamageModifiers.Coefficients.Count == 0)
            return;

        var compensator = new DamageModifierSet();
        foreach (var (damageType, c) in query.DamageModifiers.Coefficients)
        {
            if (c <= 0f || c >= 1f)
                continue;

            var multiplier = (c + penetration * (1f - c)) / c;
            compensator.Coefficients[damageType] = multiplier;
        }

        if (compensator.Coefficients.Count > 0)
            args.ModifiersList.Add(compensator);
    }
}
