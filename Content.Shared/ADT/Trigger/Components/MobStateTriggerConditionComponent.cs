using Content.Shared.Mobs;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MobStateTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true), AutoNetworkedField]
    public List<MobState> AllowedStates = new();

    [DataField]
    public LocId? FailPopup;
}
