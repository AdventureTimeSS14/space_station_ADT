using Content.Server.ADT.Ninja;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(NinjaADTConditionsSystem))]
public sealed partial class BorgHackConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int BorgsHacked;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Required = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<EntityUid> HackedBorgs = new();
}
