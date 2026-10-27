using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class InsertOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    [DataField]
    public ProtoId<OrganCategoryPrototype> ParentId = "Torso";

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (usedItem is not { } used)
            return;

        var body = entityManager.System<BodySystem>();
        var detachable = entityManager.System<DetachableOrganSystem>();

        var organEntity = used;
        if (!entityManager.HasComponent<OrganComponent>(organEntity)
            && !detachable.TryGetDetachedOrgan(used, out organEntity))
            return;

        if (!entityManager.TryGetComponent<OrganComponent>(organEntity, out var organComp) || organComp.Category != OrganId)
            return;

        if (!entityManager.HasComponent<DetachableOrganComponent>(organEntity))
        {
            body.TryInsertOrgan(patient, organEntity);
            return;
        }

        foreach (var parent in body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (parent.Comp1.Category != ParentId)
                continue;

            detachable.Attach(organEntity, patient, parent.Owner);
            return;
        }
    }
}
