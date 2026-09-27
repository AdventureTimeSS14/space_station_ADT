using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTAscendantShadowlingComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ADTShadowlingFaction";

    [DataField, AutoNetworkedField]
    public bool Phasing;

    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "ADTActionAscendantAnnihilate",
        "ADTActionAscendantHypnosis",
        "ADTActionAscendantPhaseShift",
        "ADTActionAscendantLightningStorm",
        "ADTActionAscendantBroadcast",
        "ADTActionAscendantBlackWill",
    };

    [DataField]
    public List<EntityUid> GrantedActions = new();
}
