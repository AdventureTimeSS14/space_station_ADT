using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.LegionCore;

[RegisterComponent]
public sealed partial class ADTImplantedLegionCoreComponent : Component
{
    [DataField]
    public MobState TriggerState = MobState.Critical;

    [DataField]
    public FixedPoint2 HealMin = FixedPoint2.New(20);

    [DataField]
    public FixedPoint2 HealMax = FixedPoint2.New(30);

    [DataField]
    public float CellularMultiplier = 1f;

    [DataField]
    public ProtoId<ReagentPrototype> Adrenaline = "Epinephrine";

    [DataField]
    public FixedPoint2 AdrenalineAmount = FixedPoint2.New(10);

    [DataField]
    public FixedPoint2 AdrenalineMaxLevel = FixedPoint2.New(15);

    [DataField]
    public bool LavalandOnly = true;

    [ViewVariables]
    public bool Triggered;
}
