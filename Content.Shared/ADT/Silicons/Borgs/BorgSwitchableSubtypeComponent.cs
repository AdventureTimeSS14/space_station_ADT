using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Silicons.Borgs.Components;

[RegisterComponent, NetworkedComponent,
AutoGenerateComponentState]
public sealed partial class BorgSwitchableSubtypeComponent : Component
{
    /// <summary>
    /// The <see cref="BorgSubtypePrototype"/> of this chassis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<BorgSubtypePrototype>? BorgSubtype;
}

[ByRefEvent]
public readonly record struct BorgSubtypeChangedEvent(ProtoId<BorgSubtypePrototype> Subtype);

[Serializable, NetSerializable]
public sealed class BorgSelectSubtypeMessage(ProtoId<BorgSubtypePrototype> subtype) : BoundUserInterfaceMessage
{
    public ProtoId<BorgSubtypePrototype> Subtype = subtype;
}
