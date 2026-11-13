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

    [DataField]
    public float MinDamagePercent;

    [DataField]
    public bool Repeatable = true;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.System<OrganHealthSystem>().TryGetOrgan(patient, OrganId, out var organ))
            return false;

        if (entityManager.TryGetComponent<CyberOrganComponent>(organ, out var cyber) && cyber.Decaying)
            return true;

        if (!Repeatable && organ.Comp.Operated)
            return false;

        if (organ.Comp.Damage < organ.Comp.MaxDamage * MinDamagePercent)
            return false;

        return organ.Comp.Stage >= MinStage;
    }
}
