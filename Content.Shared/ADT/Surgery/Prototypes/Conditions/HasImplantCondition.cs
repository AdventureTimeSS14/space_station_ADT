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

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (CanExtract(implant, implanted.ImplantContainer, entityManager))
                return true;
        }

        return false;
    }

    public static bool CanExtract(EntityUid implant, BaseContainer container, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent<SubdermalImplantComponent>(implant, out var subdermal) && subdermal.Permanent)
            return false;

        return entityManager.System<SharedContainerSystem>().CanRemove(implant, container);
    }
}
