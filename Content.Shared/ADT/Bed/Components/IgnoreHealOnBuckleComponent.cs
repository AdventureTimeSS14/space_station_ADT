using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Components;

/// <summary>
/// Если сущность пристёгнута к мебели с HealOnBuckleComponent,
/// она НЕ получает лечение/урон.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreHealOnBuckleComponent : Component
{
}
