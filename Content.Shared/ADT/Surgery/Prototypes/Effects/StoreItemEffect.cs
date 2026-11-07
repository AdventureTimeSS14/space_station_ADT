using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class StoreItemEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public string CavityId = string.Empty;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (usedItem is not { } item)
            return;

        var containers = entityManager.System<SharedContainerSystem>();
        var container = containers.EnsureContainer<Container>(patient, CavityId);
        containers.Insert(item, container);
    }
}
