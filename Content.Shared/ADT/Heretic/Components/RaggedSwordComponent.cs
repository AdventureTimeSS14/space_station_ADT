using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaggedSwordComponent : Component
{
    [DataField("maxHealthPerKill")]
    public float MaxHealthPerKill = 25f;
}