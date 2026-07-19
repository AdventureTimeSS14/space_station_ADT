using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Loot;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodContractComponent : Component
{
    [DataField]
    public bool Used;

    [DataField]
    public TimeSpan EffectDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public EntProtoId HunterWeapon = "ButchCleaver";

    [DataField]
    public EntProtoId? HunterObjective = "ADTBloodContractKillObjective";

    [DataField]
    public HashSet<NetEntity> ValidTargets = new();
}
