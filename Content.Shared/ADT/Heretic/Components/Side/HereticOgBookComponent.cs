using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticOgBookComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Charges = 2;

    [DataField]
    public int MaxCharges = 2;

    [DataField]
    public float RechargeTime = 120f;

    [DataField]
    public float RechargeAccumulator;
}