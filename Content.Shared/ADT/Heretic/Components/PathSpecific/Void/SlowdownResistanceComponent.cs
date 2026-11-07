//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlowdownResistanceComponent : Component
{
    [DataField]
    public float WalkBonus = 1.2f;

    [DataField]
    public float SprintBonus = 1.2f;
}
