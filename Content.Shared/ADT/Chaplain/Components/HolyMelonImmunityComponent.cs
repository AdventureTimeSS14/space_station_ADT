using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

/// <summary>
/// Компонент для предметов (например, святой арбуз), которые дают иммунитет к магии,
/// когда находятся в руке владельца.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolyMelonImmunityComponent : Component
{
}
