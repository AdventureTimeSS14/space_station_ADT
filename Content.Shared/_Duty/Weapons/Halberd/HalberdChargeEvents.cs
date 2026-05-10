using Content.Shared.Actions;
using Robust.Shared.Map;

namespace Content.Shared._Duty.Weapons.Halberd;

/// <summary>
/// Событие активации рывка алебарды через хотбар.
/// WorldTarget — игрок указывает курсором направление.
/// </summary>
public sealed partial class HalberdChargeActionEvent : WorldTargetActionEvent
{
}
