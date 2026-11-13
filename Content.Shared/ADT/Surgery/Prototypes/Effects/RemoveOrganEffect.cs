using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class RemoveOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();

        foreach (var organ in body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category != OrganId)
                continue;

            body.TryRemoveOrgan(organ.Owner);
            break;
        }
    }
}
