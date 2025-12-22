using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.RunAndGunSpreadModifier;

[RegisterComponent, NetworkedComponent]
public sealed partial class RunAndGunBlockerComponent : Component
{
    [DataField]
    public float MaxVelocity = 4.6f;
}
