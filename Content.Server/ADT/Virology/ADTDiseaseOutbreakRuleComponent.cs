using Content.Shared.ADT.Virology.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Virology;

[RegisterComponent, Access(typeof(ADTDiseaseOutbreakRule))]
public sealed partial class ADTDiseaseOutbreakRuleComponent : Component
{
    [DataField]
    public List<ProtoId<ADTDiseasePrototype>> Diseases = new();

    [DataField]
    public float AdvanceChance = 0.25f;
}
