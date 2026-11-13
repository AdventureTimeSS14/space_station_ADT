using Content.Shared.ADT.Body.Shrapnel;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class EmbeddedObjectsCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();
        var containers = entityManager.System<SharedContainerSystem>();

        foreach (var organ in body.EnumerateOrgans<ADTEmbeddedObjectsComponent>(patient))
        {
            if (organ.Comp1.Category != OrganId)
                continue;

            if (containers.TryGetContainer(organ.Owner, ADTEmbeddedObjectsComponent.ContainerId, out var container)
                && container.ContainedEntities.Count > 0)
                return true;
        }

        return false;
    }
}
