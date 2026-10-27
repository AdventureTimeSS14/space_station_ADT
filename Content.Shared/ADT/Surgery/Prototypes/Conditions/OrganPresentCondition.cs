using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class OrganPresentCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();

        foreach (var organ in body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category == OrganId)
                return true;
        }

        return false;
    }
}
