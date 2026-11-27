//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent]
public sealed partial class FireStackSpeedComponent : Component
{
    [DataField]
    public float FireStackSpeedMultiplier = 0.02f;
}
