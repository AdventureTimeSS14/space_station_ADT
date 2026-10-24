using Content.Shared.Chemistry.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InjectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public Solution Solution = default!;

    [DataField, AutoNetworkedField]
    public InjectDestination Destination = InjectDestination.Bloodstream;

    [DataField]
    public LocId? FullPopup;
}

[Serializable, NetSerializable]
public enum InjectDestination : byte
{
    Bloodstream,
    Stomach,
}
