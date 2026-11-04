using Content.Shared.ADT.Body.Bones;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class FractureCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        return entityManager.System<SharedADTBoneSystem>().HasFracture(patient, OrganId);
    }
}
