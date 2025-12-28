using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Cryogenics;

/// <summary>
/// Когда сущность имеет этот компонент — криопод НЕ может
/// лечить, вредить или вводить реагенты, но температура
/// продолжает изменяться.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreCryopodsComponent : Component
{
}
