using Content.Server.ADT.Ninja;
using Content.Server.Objectives.Systems;
using Content.Shared.ADT.Ninja.Components;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(BrainExtractorSystem), typeof(NinjaADTConditionsSystem))]
public sealed partial class BrainScanConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ScansCompleted;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxScans = 2;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> ScannedMinds = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> ScannedBodies = new();
}
