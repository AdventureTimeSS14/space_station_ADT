using Content.Shared.ADT.Shadowling;
using Content.Shared.Body;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class RemoveShadowTumorEffect : SurgeryStepEffect
{
    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (entityManager.HasComponent<ADTLesserShadowlingComponent>(patient))
        {
            if (surgeon is { } user)
            {
                var ev = new ADTLesserTumorBacklashEvent(user);
                entityManager.EventBus.RaiseLocalEvent(patient, ref ev);
            }

            return;
        }

        var body = entityManager.System<BodySystem>();
        foreach (var organ in body.EnumerateOrgans<ADTShadowTumorComponent>(patient))
        {
            body.TryRemoveOrgan(organ.Owner);
            break;
        }
    }
}
