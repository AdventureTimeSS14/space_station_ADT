using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Effects;

[Serializable, NetSerializable]
public sealed partial class RepairOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganId;

    [DataField]
    public float HealTo = 0.1f;

    public override void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var health = entityManager.System<OrganHealthSystem>();
        if (!health.TryGetOrgan(patient, OrganId, out var organ))
            return;

        if (entityManager.TryGetComponent<CyberOrganComponent>(organ, out var cyber))
            cyber.Decaying = false;

        health.SetDamage((organ.Owner, organ.Comp), Math.Min(organ.Comp.Damage, organ.Comp.MaxDamage * HealTo));
    }
}
