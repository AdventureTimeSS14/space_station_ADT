using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hierophant;

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantArenaComponent : Component
{
    [DataField]
    public float Radius = 12f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantForcedTileMovementComponent : Component
{
}
