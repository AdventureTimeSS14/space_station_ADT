using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

/// <summary>
/// Временный маркер иммунитета к магии от святого арбуза.
/// Удаляется при выпуске арбуза из руки.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolyMelonMagicImmunityComponent : Component
{
}
