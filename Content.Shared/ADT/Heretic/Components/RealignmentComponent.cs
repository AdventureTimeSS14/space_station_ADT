//

using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RealignmentComponent : Component
{
    [DataField]
    public string StaminaRegenKey = "Realignment";
}
