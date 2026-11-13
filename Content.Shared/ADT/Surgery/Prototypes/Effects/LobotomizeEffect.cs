using Content.Shared.ADT.Surgery.Components;
using Content.Shared.Body;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class LobotomizeEffect : SurgeryStepEffect
{
    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();

        foreach (var organ in body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category == "Brain")
                entityManager.EnsureComponent<LobotomizedOrganComponent>(organ.Owner);
        }
    }
}
