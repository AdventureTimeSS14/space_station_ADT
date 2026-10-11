using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Cyberpsychosis;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveCyberpsychosisComponent : Component
{
    [DataField, AutoNetworkedField]
    public CyberpsychosisState State = CyberpsychosisState.None;
}
