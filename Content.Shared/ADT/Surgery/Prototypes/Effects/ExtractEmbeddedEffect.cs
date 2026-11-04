using Content.Shared.ADT.Body.Shrapnel;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class ExtractEmbeddedEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();
        var containers = entityManager.System<SharedContainerSystem>();
        var transform = entityManager.System<SharedTransformSystem>();
        var hands = entityManager.System<SharedHandsSystem>();

        foreach (var organ in body.EnumerateOrgans<ADTEmbeddedObjectsComponent>(patient))
        {
            if (organ.Comp1.Category != OrganId
                || !containers.TryGetContainer(organ.Owner, ADTEmbeddedObjectsComponent.ContainerId, out var container))
                continue;

            foreach (var item in new List<EntityUid>(container.ContainedEntities))
            {
                transform.DropNextTo(item, patient);
                if (surgeon != null)
                    hands.TryPickupAnyHand(surgeon.Value, item);
            }
        }
    }
}
