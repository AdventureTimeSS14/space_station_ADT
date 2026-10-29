using Content.Shared.ADT.Surgery.Components;
using Content.Shared.Body;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class LobotomizedCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();

        foreach (var organ in body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category == "Brain" && entityManager.HasComponent<LobotomizedOrganComponent>(organ.Owner))
                return true;
        }

        return false;
    }
}
