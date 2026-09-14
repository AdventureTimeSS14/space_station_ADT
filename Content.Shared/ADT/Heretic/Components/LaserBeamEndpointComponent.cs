using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LaserBeamEndpointComponent : Component
{
    [DataField]
    public bool PvsOverride = true;
}
