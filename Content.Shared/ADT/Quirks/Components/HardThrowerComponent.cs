using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Traits;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class HardThrowerComponent : Component
{
    [DataField]
    public float Modifier = 0.8f;
}
