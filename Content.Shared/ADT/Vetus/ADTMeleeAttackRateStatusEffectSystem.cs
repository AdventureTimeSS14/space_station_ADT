using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Vetus;

public sealed class ADTMeleeAttackRateStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeWeaponComponent, GetMeleeAttackRateEvent>(OnGetAttackRate);
    }

    private void OnGetAttackRate(Entity<MeleeWeaponComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (!_statusEffects.TryEffectsWithComp<ADTMeleeAttackRateStatusEffectComponent>(args.User, out var effects))
            return;

        foreach (var effect in effects)
        {
            args.Multipliers *= effect.Comp1.Multiplier;
        }
    }
}
