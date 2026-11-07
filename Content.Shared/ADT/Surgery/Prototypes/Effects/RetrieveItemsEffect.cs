using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class RetrieveItemsEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public string CavityId = string.Empty;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var containers = entityManager.System<SharedContainerSystem>();
        if (!containers.TryGetContainer(patient, CavityId, out var container))
            return;

        var coordinates = entityManager.GetComponent<TransformComponent>(patient).Coordinates;
        containers.EmptyContainer(container, true, coordinates);
    }
}
