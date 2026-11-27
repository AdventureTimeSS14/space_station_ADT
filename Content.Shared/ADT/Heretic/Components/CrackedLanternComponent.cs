//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrackedLanternComponent : Component
{
    [DataField]
    public float Range = 4f;

    [DataField]
    public float Duration = 4f;

    [DataField]
    public float FireStacks = 2f;
}
