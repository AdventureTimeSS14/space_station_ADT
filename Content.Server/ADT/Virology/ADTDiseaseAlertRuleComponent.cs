using Content.Shared.ADT.Virology;

namespace Content.Server.ADT.Virology;

[RegisterComponent, Access(typeof(ADTDiseaseAlertRule))]
public sealed partial class ADTDiseaseAlertRuleComponent : Component
{
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

    [ViewVariables]
    public int Stage;

    [ViewVariables]
    public TimeSpan NextCheck;
}
