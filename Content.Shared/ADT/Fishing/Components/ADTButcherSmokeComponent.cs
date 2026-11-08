using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTButcherSmokeComponent : Component
{
    [DataField]
    public EntProtoId Prototype = "Smoke";

    [DataField]
    public Solution Solution = new();

    [DataField]
    public float Duration = 10f;

    [DataField]
    public int SpreadAmount = 12;
}
