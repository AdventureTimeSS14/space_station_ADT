using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class InsertOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (usedItem is not { } organEntity)
            return;

        if (!entityManager.TryGetComponent<OrganComponent>(organEntity, out var organComp) || organComp.Category != OrganId)
            return;

        var body = entityManager.System<BodySystem>();
        body.TryInsertOrgan(patient, organEntity);
    }
}
