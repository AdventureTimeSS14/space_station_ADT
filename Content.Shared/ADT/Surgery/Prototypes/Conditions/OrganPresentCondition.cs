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

        if (!body.TryGetOrgansWithComponent<OrganComponent>(patient, out var organs))
            return false;

        foreach (var organ in organs)
        {
            if (organ.Comp.Category == OrganId)
                return true;
        }

        return false;
    }
}
