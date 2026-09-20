using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Trigger;

/// <summary>
/// Полностью восстанавливает стамину цели и выводит её из стамкрита.
/// Если TargetUser — цель — пользователь.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaRestoreOnTriggerComponent : BaseXOnTriggerComponent;
