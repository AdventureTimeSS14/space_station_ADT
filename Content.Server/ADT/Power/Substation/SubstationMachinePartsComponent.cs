using Content.Shared.ADT.Construction;

namespace Content.Server.ADT.Power.Substation;

[RegisterComponent, Access(typeof(SubstationMachinePartsSystem))]
public sealed partial class SubstationMachinePartsComponent : Component
{
    [DataField]
    public float BaseMaxSupply = 150000f;

    [DataField]
    public float BaseMaxChargeRate = 5000f;

    [DataField]
    public float BaseMaxCharge = 2500000f;
}