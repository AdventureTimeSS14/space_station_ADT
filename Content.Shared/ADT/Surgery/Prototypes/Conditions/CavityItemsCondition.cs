using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class CavityItemsCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public string CavityId = string.Empty;

    [DataField]
    public int Min;

    [DataField]
    public int Max = int.MaxValue;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var count = 0;

        if (entityManager.System<SharedContainerSystem>().TryGetContainer(patient, CavityId, out var container))
            count = container.ContainedEntities.Count;

        return count >= Min && count <= Max;
    }
}
