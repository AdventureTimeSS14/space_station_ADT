using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryStepEffect
{
    public abstract void Apply(EntityUid patient, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager);
}
