using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
}