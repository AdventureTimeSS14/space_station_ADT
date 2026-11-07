using Robust.Shared.GameStates;
namespace Content.Shared.ADT.Heretic.Components.Side;
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticMansusGateComponent : Component
{
    [DataField]
    public EntityUid? LinkedGate;
}
