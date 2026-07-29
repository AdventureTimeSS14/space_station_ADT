using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hierophant;

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantArenaComponent : Component
{
    [DataField]
    public float Radius = 12f;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextCheckAt;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantArenaTileMovementComponent : Component
{
}
