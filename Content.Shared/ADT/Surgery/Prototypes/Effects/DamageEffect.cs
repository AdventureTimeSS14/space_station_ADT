using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class DamageEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var damageable = entityManager.System<DamageableSystem>();
        damageable.TryChangeDamage(patient, Damage, origin: surgeon);
    }
}
