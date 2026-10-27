using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class ExtractImplantEffect : SurgeryStepEffect
{
    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ImplantedComponent>(patient, out var implanted))
            return;

        var container = entityManager.System<SharedContainerSystem>();
        foreach (var implant in implanted.ImplantContainer.ContainedEntities.ToArray())
        {
            if (!container.CanRemove(implant, implanted.ImplantContainer))
                continue;

            container.Remove(implant, implanted.ImplantContainer);

            if (surgeon is { } user)
                entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, implant);

            return;
        }
    }
}
