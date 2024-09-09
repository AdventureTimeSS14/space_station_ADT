using Robust.Shared.GameStates;

namespace Content.Shared.ADT.NoShowFov;

/// <summary>
///     Applies a fog of war effect to a unit when this component is equipped to the eyes, head, or mask slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoShowFovComponent : Component { }
