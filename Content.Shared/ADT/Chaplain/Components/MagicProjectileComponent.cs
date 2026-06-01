using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

/// <summary>
/// Компонент для магических снарядов, которые должны проверять иммунитет к магии при попадании.
/// Если цель имеет иммунитет к магии, снаряд поглощается без эффекта.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MagicProjectileComponent : Component
{
}
