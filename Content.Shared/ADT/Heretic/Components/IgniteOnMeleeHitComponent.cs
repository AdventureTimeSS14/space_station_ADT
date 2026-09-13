//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnMeleeHitComponent : Component
{
    [DataField]
    public float FireStacks = 2f;
}
