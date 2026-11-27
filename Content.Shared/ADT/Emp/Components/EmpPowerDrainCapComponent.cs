using Robust.Shared.GameStates;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpPowerDrainCapComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RelativeToMaxCharge = true;

    [DataField, AutoNetworkedField]
    public float CapPercentage = 0.5f;
}
