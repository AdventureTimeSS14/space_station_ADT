using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Silicons.Borgs;

[RegisterComponent]
public sealed partial class BorgInfoComponent : Component
{
    public EntityUid? BatteryUid;
    [DataField]
    public string ContainerId = "cell_slot";

    [DataField]
    public EntityUid? Target;
    [DataField]
    public EntProtoId Action = "ActionBorgInfo";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public string? StationName;
    [ViewVariables]
    public string? StationAlertLevel;
    [ViewVariables]
    public Color StationAlertColor = Color.White;
}
