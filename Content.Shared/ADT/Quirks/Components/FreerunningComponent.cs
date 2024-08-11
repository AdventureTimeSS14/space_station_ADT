using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Traits;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class FreerunningComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Modifier = 0.6f;
}
