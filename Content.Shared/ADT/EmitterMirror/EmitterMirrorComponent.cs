using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Shared.ADT.EmitterMirror;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class EmitterMirrorComponent : Component
{
    /// <summary>
    /// Directions that are blocked for reflection
    /// </summary>
    [DataField]
    public List<string> BlockedDirections = new();

    /// <summary>
    /// Reflection direction for Trinary reflector type
    /// </summary>
    [DataField]
    public Direction? TrinaryMirrorDirection;

    /// <summary>
    /// Binary reflector type, uses a simple reflection
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BinaryReflector = true;

    /// <summary>
    /// Trinary reflector type, uses a predefined reflection direction
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TrinaryReflector = false;

    /// <summary>
    /// Whitelist for projectiles that can be reflected
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Direction to Vector2
    /// </summary>
    public static readonly Dictionary<Direction, Vector2> DirectionToVector = new()
    {
        { Direction.North,  Vector2.UnitY  },
        { Direction.South, -Vector2.UnitY },
        { Direction.East,  -Vector2.UnitX },
        { Direction.West,   Vector2.UnitX }
    };
}