using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TimeDespawnDamage;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class TimeDespawnDamageComponent : Component
{
    [DataField]
    public int Count = 0;
}
