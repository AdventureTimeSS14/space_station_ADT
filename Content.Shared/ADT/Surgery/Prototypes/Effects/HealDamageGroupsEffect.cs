using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class HealDamageGroupsEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public List<ProtoId<DamageGroupPrototype>> Groups = new();

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var damageable = entityManager.System<DamageableSystem>();

        foreach (var group in Groups)
        {
            damageable.HealDistributed(patient, -Amount, group, surgeon);
        }
    }
}
