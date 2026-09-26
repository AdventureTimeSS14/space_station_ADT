using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveStatusEffectsOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Effects = new();
}
