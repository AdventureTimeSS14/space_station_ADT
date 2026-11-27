using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// A basic raycast system that will shoot in a straight line when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicRaycastComponent : Component
{
    /// <summary>
    /// Maximum distance the raycast will travel before giving up. Reflections will reset the distance traveled
    /// </summary>
    [DataField]
    public float MaxDistance = 20.0f;

    /// <summary>
    /// The collision mask the hitscan ray uses to collide with other objects. See the enum for more information
    /// </summary>
    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.Opaque;

    /// <summary>
    /// ADT hitscan #3142: how many aim-pointer selection attempts to make when choosing a hit.
    /// </summary>
    [DataField]
    public int Steps = 3;

    /// <summary>
    /// Starlight: hits closer than this are ignored unless the target has one of <see cref="NotArmedCollideWith"/>.
    /// </summary>
    [DataField]
    public float MinDistance;

    [DataField]
    public List<ProtoId<TagPrototype>> NotArmedCollideWith = new();
}
