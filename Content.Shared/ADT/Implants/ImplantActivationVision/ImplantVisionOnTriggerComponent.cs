using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.ImplantActivationVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantVisionOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);
}
