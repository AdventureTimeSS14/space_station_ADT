using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrusherDashActiveComponent : Component
{
    [DataField]
    public EntProtoId ActionProto = "ActionADTCrusherDash";

    [ViewVariables]
    public EntityUid? ActionUid;

    [ViewVariables]
    public TimeSpan ExpireTime;
}
