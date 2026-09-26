using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTSoakableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Water";

    [DataField]
    public FixedPoint2 Amount = 10;
}
