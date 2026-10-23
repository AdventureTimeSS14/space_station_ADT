using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Implants.ImplantVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ImplantVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ImplantVisionPrototype>, ImplantVisionState> Effects = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ImplantVisionState
{
    [DataField]
    public TimeSpan Start;

    [DataField]
    public TimeSpan? End;
}
