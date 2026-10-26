using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MechToolComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 EnergyCost = 20;
}
