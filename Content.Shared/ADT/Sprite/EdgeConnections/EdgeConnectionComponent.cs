using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Sprite.EdgeConnections;

/// <summary>
/// Marks entities that should visually connect to adjacent peers with the same key.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EdgeConnectionComponent : Component
{
    /// <summary>
    /// Only entities with equal keys can form a visual connection.
    /// </summary>
    [DataField]
    public string ConnectionKey = "default";

    /// <summary>
    /// Relative directions this entity can connect in.
    /// These directions are rotated by the entity transform.
    /// </summary>
    [DataField]
    public EdgeConnectionDirections AllowedDirections = EdgeConnectionDirections.None;
}
