using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.ImplantBoost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantBoostOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true)]
    public ImplantBoostSettings Settings = new();

    [DataField]
    public TimeSpan? Duration;
}
