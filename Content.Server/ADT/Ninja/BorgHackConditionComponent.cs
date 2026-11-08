using Content.Server.ADT.Ninja;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(NinjaADTConditionsSystem), typeof(NinjaBorgSystem))]
public sealed partial class BorgHackConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int BorgsHacked;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Required = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> HackedBorgs = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Target = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Min = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Max = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinFactor = 0f, MaxFactor = 0f;
}
