using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Armor;

public abstract partial class SharedArmorSystem
{
    public float GetArmorCoefficient(EntityUid item, ProtoId<DamageTypePrototype> damageType)
    {
        if (!TryComp<ArmorComponent>(item, out var armor))
            return 1f;

        return armor.Modifiers.Coefficients.GetValueOrDefault(damageType, 1f);
    }
}
