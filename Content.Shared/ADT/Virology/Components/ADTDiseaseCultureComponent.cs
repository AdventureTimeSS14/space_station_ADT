using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Components;

[RegisterComponent]
public sealed partial class ADTDiseaseCultureComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ADTDiseasePrototype> Disease;

    [DataField]
    public string Solution = "drink";

    [DataField]
    public FixedPoint2 Amount = FixedPoint2.New(20);
}
