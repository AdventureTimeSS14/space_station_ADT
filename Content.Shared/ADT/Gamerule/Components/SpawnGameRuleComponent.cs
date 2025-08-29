using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.GameRules.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpawnGameRuleComponent : Component
{
    [DataField("gameRule", required: true), AutoNetworkedField]
    public string GameRuleProto = string.Empty;

    [DataField("startImmediately"), AutoNetworkedField]
    public bool StartImmediately = true;
}