using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SpaceWhale;

/// <summary>
/// When given to an entity, creates X tailed entities that try to follow the entity with the component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TailedEntityComponent : Component
{
    /// <summary>
    /// amount of entities in between the tail and the head
    /// </summary>
    [DataField]
    public int Amount = 3;

    /// <summary>
    /// the entity/entities that should be spawned after the head
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// How much space between entities
    /// </summary>
    [DataField]
    public float Spacing = 1f;

    /// <summary>
    /// Client-only visual smoothing factor. Larger values make tails visually follow faster (interpolate per-frame).
    /// This is purely visual and does not change server authoritative positions.
    /// </summary>
    [DataField]
    public float Smoothing = 10f;

    /// <summary>
    /// List of tail segments
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<NetEntity> TailSegments = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 LastPos = Vector2.Zero;
}
