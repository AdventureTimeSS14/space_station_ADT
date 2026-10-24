using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDamageThresholdComponent : BaseTriggerOnXComponent
{
    [DataField, AutoNetworkedField]
    public float MarginBeforeCrit = 20f;

    public bool WasTriggered;
}
