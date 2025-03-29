using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Sewing;

/// <summary>
/// Пустой компонент, не имеющий функционала
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class SewingComponent : Component
{
}