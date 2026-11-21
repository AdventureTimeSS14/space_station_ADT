//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MetabolismModifierComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
