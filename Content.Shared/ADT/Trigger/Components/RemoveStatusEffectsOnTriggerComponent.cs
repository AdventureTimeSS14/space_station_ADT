using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Trigger;

/// <summary>
/// Снимает перечисленные статус-эффекты с цели (например, оглушение).
/// Если TargetUser — цель — пользователь.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveStatusEffectsOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Прототипы статус-эффектов, которые будут сняты.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Effects = new();
}
