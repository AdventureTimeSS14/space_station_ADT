using Robust.Shared.GameObjects;

namespace Content.Shared.Destructible;

/// <summary>
/// When this entity is destroyed, destructible thresholds should NOT spawn scraps
/// because the entity is being crushed/covered by the pod.
/// </summary>
[RegisterComponent]
public sealed partial class DroppodSuppressedComponent : Component
{
}
