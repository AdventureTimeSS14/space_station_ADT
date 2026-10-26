using Content.Shared.FixedPoint;

namespace Content.Shared.ADT.Lavaland.LegionCore;

[RegisterComponent]
public sealed partial class ADTLegionCoreToleranceComponent : Component
{
    [DataField]
    public int Uses;

    [DataField]
    public FixedPoint2 CellularBase = FixedPoint2.New(10);

    [DataField]
    public FixedPoint2 CellularStep = FixedPoint2.New(7);

    [DataField]
    public FixedPoint2 CellularMax = FixedPoint2.New(50);

    [DataField]
    public int WarningUses = 4;
}
