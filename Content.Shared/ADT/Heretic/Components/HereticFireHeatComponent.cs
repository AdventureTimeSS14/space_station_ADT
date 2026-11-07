using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticFireHeatComponent : Component
{
    [DataField]
    public TimeSpan Expires = TimeSpan.Zero;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextTick = TimeSpan.Zero;

    [DataField]
    public float DamagePerTick = 2f;
}