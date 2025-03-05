using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weather;

/// <summary>
/// A component added to map to add a "wind" effect
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeavyWindComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Vector2 Direction = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Speed = 2f;
}
