using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticOgBookComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Charges = 5;

    [DataField]
    public int MaxCharges = 5;

    [DataField]
    public float RechargeTime = 60f;

    [DataField]
    public float RechargeAccumulator;
}