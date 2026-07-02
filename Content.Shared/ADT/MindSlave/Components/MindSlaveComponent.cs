using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MindSlave.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindSlaveComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "MindSlaveFaction";

    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public EntityUid Master;

    [DataField, AutoNetworkedField]
    public string MasterName = string.Empty;
}
