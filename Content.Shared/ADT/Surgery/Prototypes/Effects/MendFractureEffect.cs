using Content.Shared.ADT.Body.Bones;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class MendFractureEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var bones = entityManager.System<SharedADTBoneSystem>();
        if (bones.TryGetBone(patient, OrganId, out var bone))
            bones.Mend(patient, bone.Value);
    }
}
