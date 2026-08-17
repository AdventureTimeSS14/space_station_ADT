using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Medical.StasisBed;

[RegisterComponent]
[Access(typeof(StasisBedRotSystem))]
public sealed partial class StasisBedRotComponent : Component
{
    [DataField]
    public float RotStopTier = 2f;

    [DataField]
    public float InaprovalineTier = 3f;

    [DataField]
    public FixedPoint2 InaprovalineAmount = FixedPoint2.New(0.5f);

    [DataField]
    public ProtoId<ReagentPrototype> InaprovalineReagent = "Inaprovaline";

    [ViewVariables(VVAccess.ReadWrite)]
    public float Tier = 1f;
}
