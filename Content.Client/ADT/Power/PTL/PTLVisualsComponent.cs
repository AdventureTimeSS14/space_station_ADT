namespace Content.Client.ADT.Power.PTL;

[RegisterComponent]
public sealed partial class PTLVisualsComponent : Component
{
    [DataField] public string ChargePrefix = "charge-";
    [DataField] public int MaxChargeStates = 6;
}
