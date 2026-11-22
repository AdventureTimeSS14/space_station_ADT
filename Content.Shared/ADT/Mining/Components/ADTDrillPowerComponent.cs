using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTDrillPowerComponent : Component
{
    [DataField]
    public int Power = 1;
}
