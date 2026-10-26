using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAscendantPhasedComponent : Component
{
    [DataField]
    public EntProtoId UnphaseAction = "ADTActionAscendantUnphase";

    [DataField]
    public List<EntityUid> GrantedActions = new();
}
