using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTShadowlingThrallComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ADTShadowlingThrallFaction";

    [DataField, AutoNetworkedField]
    public EntityUid? Master;

    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "ADTActionShadowlingGuise",
        "ADTActionShadowlingDarksight",
    };

    [DataField]
    public List<EntityUid> GrantedActions = new();

    [DataField]
    public LocId ExamineText = "shadowling-thrall-examine";

    [DataField]
    public TimeSpan DethrallTime = TimeSpan.FromSeconds(10);

    [DataField]
    public EntProtoId TumorProto = "ADTShadowTumor";
}
