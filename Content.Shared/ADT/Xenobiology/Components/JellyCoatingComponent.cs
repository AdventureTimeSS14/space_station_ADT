using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class JellyCoatingComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1.3f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SpeedBoostedComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1.3f;
}
