//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnFlashComponent : Component
{
    [DataField]
    public float FireStacks;

    [DataField]
    public float FireProtectionPenetration;
}
