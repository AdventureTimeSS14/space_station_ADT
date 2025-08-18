using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Ghost;

[RegisterComponent]
public sealed partial class GhostInfoComponent : Component
{
    [DataField]
    public EntityUid? Target;
    [DataField("Info")]
    public EntProtoId Action = "ActionGhostInfo";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public string? StationAlertLevel;
    [ViewVariables]
    public Color StationAlertColor = Color.White;

}
