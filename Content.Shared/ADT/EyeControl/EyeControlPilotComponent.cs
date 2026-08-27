using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EyeControl;

[RegisterComponent, NetworkedComponent]
public sealed partial class EyeControlPilotComponent : Component
{
    [DataField(required: true)]
    public EntityUid Console;

    [DataField(required: true)]
    public EntityUid Eye;

    [DataField]
    public Dictionary<EntProtoId, EntityUid?> Actions = new();

    [ViewVariables]
    public Angle PreviousRelativeRotation;

    [ViewVariables]
    public Angle PreviousTargetRelativeRotation;
}
