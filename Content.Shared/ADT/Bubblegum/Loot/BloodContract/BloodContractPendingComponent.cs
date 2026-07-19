using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Loot;

[RegisterComponent]
public sealed partial class BloodContractPendingComponent : Component
{
    [DataField]
    public TimeSpan ApplyAt;

    [DataField]
    public EntProtoId HunterWeapon = "ButchCleaver";

    [DataField]
    public EntProtoId? HunterObjective = "ADTBloodContractKillObjective";
}
