using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class OrganDamagedCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    [DataField]
    public OrganStage MinStage = OrganStage.Bruised;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.System<OrganHealthSystem>().TryGetOrgan(patient, OrganId, out var organ))
            return false;

        return organ.Comp.Stage >= MinStage
               || entityManager.TryGetComponent<CyberOrganComponent>(organ, out var cyber) && cyber.Decaying;
    }
}
