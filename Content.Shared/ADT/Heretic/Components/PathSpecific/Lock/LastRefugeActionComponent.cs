//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent]
public sealed partial class LastRefugeActionComponent : Component
{
    [DataField]
    public float OtherMindsCheckRange = 5f;
}
