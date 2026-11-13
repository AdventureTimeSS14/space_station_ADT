using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class DamageGroupsCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public List<ProtoId<DamageGroupPrototype>> Groups = new();

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var damage = entityManager.System<DamageableSystem>().GetDamagePerGroup(patient);

        foreach (var group in Groups)
        {
            if (damage.TryGetValue(group, out var amount) && amount > FixedPoint2.Zero)
                return true;
        }

        return false;
    }
}
