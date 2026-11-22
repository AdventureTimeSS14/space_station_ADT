using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Training;

[RegisterComponent, NetworkedComponent]
public sealed partial class TrainingEquipmentComponent : Component
{
    [DataField]
    public TimeSpan MaxCreditPerAction = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan FirstActionCredit = TimeSpan.FromSeconds(3);

    [DataField(required: true)]
    public List<TrainingTier> Tiers = new();
}

[DataDefinition]
public sealed partial class TrainingTier
{
    [DataField(required: true)]
    public TimeSpan Time;

    [DataField]
    public float StaminaBonus;

    [DataField]
    public float UnarmedDamageBonus;

    [DataField]
    public float PullDensityReduction;
}
