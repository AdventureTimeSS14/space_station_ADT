using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class HasImplantCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ImplantedComponent>(patient, out var implanted))
            return false;

        var container = entityManager.System<SharedContainerSystem>();
        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (container.CanRemove(implant, implanted.ImplantContainer))
                return true;
        }

        return false;
    }
}
