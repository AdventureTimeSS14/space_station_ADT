using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.RunAndGunSpreadModifier;

[RegisterComponent, NetworkedComponent]
public sealed partial class RunAndGunSpreadModifierComponent : Component
{
    [DataField]
    public int Modifyer = 1;
}
