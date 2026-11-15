using Robust.Shared.GameStates;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpPowerDrainCapComponent : Component
{
    [DataField]
    public bool RelativeToMaxCharge = true;

    [DataField]
    public float CapPercentage = 0.5f;
}
