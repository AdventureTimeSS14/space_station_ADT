using System.Linq;
using Content.Shared.ADT.Surgery.Prototypes.Conditions;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class ExtractImplantEffect : SurgeryStepEffect
{
    [DataField]
    public List<EntProtoId> Include = new();

    [DataField]
    public List<EntProtoId> Exclude = new();

    [DataField]
    public ProtoId<OrganCategoryPrototype>? OrganId;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (!HasImplantCondition.TryGetImplanted(patient, OrganId, entityManager, out var implanted))
            return;

        var container = entityManager.System<SharedContainerSystem>();
        foreach (var implant in implanted.ImplantContainer.ContainedEntities.ToArray())
        {
            if (!HasImplantCondition.CanExtract(implant, implanted.ImplantContainer, entityManager, Include, Exclude))
                continue;

            container.Remove(implant, implanted.ImplantContainer, destination: entityManager.GetComponent<TransformComponent>(patient).Coordinates);

            if (surgeon is { } user)
                entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, implant);

            return;
        }
    }
}
