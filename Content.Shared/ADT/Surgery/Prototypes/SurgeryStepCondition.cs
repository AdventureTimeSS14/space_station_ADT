using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class SurgeryStepCondition
{
    [DataField]
    public bool Inverted;

    public bool CheckWithInvert(EntityUid patient, IEntityManager entityManager)
    {
        var result = Check(patient, entityManager);
        return Inverted ? !result : result;
    }

    public abstract bool Check(EntityUid patient, IEntityManager entityManager);
}
