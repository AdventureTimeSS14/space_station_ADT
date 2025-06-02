using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Silicons.Borgs.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class BorgSwitchableSubtypeComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<BorgSubtypePrototype>? BorgSubtype;
}

[Serializable, NetSerializable]
public sealed class BorgSelectSubtypeMessage(ProtoId<BorgSubtypePrototype> subtype) : BoundUserInterfaceMessage
{
    public ProtoId<BorgSubtypePrototype> Subtype = subtype;
}
