using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class OrganKindCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    [DataField(required: true)]
    public OrganKind Kind;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        return entityManager.System<OrganHealthSystem>().TryGetOrgan(patient, OrganId, out var organ)
               && organ.Comp.Kind == Kind;
    }
}
