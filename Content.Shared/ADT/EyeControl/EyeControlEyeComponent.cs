using Content.Shared.ADT.Areas;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EyeControl;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false)]
public sealed partial class EyeControlEyeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityUid Pilot;

    [DataField(required: true), AutoNetworkedField]
    public EntityUid Console;

    [DataField, AutoNetworkedField]
    public EntProtoId<AreaComponent>? AllowedArea;

    [ViewVariables]
    public bool IsProcessingMoveEvent;
}
