using Content.Shared.ADT.Virology;
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

    [DataField]
    public bool InfectAtStart = true;

    [DataField]
    public bool Announce = true;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public float SecondStageFraction = 0.35f;

    [DataField]
    public int SecondStageMinimum = 8;

    [DataField]
    public LocId OutbreakMessage = "adt-disease-alert-outbreak";

    [DataField]
    public LocId ConfirmedMessage = "adt-disease-alert-confirmed";

    [DataField]
    public ADTDiseaseSeverity MinAnnounceSeverity = ADTDiseaseSeverity.Minor;

    [ViewVariables]
    public int Stage;

    [ViewVariables]
    public TimeSpan NextCheck;
}
