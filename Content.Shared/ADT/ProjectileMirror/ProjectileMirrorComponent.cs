using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Shared.ADT.ProjectileMirror;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ProjectileMirrorComponent : Component
{
    /// <summary> 
    /// Directions that block reflection. 
    /// </summary>
    [DataField]
    public List<string> ExitSide = new();

    /// <summary>
    ///  Reflection direction for Trinary reflector. 
    /// </summary>
    [DataField]
    public Direction? TrinaryMirrorDirection;

    /// <summary>
    ///  Simple binary reflection toggle. 
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BinaryReflection = true;

    /// <summary>
    ///  Predefined direction reflection toggle. 
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TrinaryReflection = false;

    /// <summary> 
    /// Whitelist for reflectable projectiles. 
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Just sides list
    /// </summary>
    public static readonly Dictionary<Direction, Vector2> DirectionToVector = new()
    {
        { Direction.North,  Vector2.UnitY  },
        { Direction.South, -Vector2.UnitY },
        { Direction.East,  -Vector2.UnitX },
        { Direction.West,   Vector2.UnitX }
    };
}