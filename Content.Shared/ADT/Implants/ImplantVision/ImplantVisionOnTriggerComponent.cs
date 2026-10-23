using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Implants.ImplantVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantVisionOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ImplantVisionPrototype> Vision;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);
}
