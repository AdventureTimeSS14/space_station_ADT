using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MindSlave.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindSlaveMasterComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "MindSlaveMasterFaction";
}
