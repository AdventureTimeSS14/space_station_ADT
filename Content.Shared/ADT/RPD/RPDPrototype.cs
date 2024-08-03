using Content.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.RPD;

/// <summary>
/// Contains the parameters for a RPD construction / operation
/// </summary>
[Prototype("RPD")]
public sealed partial class RPDPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The RPD mode associated with the operation
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public RpdMode Mode { get; private set; } = RpdMode.Invalid;

    /// <summary>
    /// The name associated with the prototype
    /// </summary>
    [DataField("name"), ViewVariables(VVAccess.ReadOnly)]
    public string SetName { get; private set; } = "Unknown";

    /// <summary>
    /// The name of the radial container that this prototype will be listed under on the RPD menu
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string Category { get; private set; } = "Undefined";

    /// <summary>
    /// Texture path for this prototypes menu icon
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SpriteSpecifier? Sprite { get; private set; } = null;

    /// <summary>
    /// The entity prototype that will be constructed (mode dependent)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string? Prototype { get; private set; } = string.Empty;

    /// <summary>
    /// Number of charges consumed when the operation is completed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Cost { get; private set; } = 1;

    /// <summary>
    /// The length of the operation
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Delay { get; private set; } = 1f;

    /// <summary>
    /// The visual effect that plays during this operation
    /// </summary>
    [DataField("fx"), ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId? Effect { get; private set; } = null;

    /// <summary>
    /// A list of rules that govern where the entity prototype can be contructed
    /// </summary>
    [DataField("rules"), ViewVariables(VVAccess.ReadOnly)]
    public HashSet<RpdConstructionRule> ConstructionRules { get; private set; } = new();

    /// <summary>
    /// The collision mask used for determining whether the entity prototype will fit into a target tile
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public CollisionGroup CollisionMask { get; private set; } = CollisionGroup.None;

    /// <summary>
    /// Specifies a set of custom collision bounds for determining whether the entity prototype will fit into a target tile
    /// </summary>
    /// <remarks>
    /// Should be set assuming that the entity faces south.
    /// Make sure that Rotation is set to RpdRotation.User if the entity is to be rotated by the user
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Box2? CollisionBounds
    {
        get
        {
            return _collisionBounds;
        }

        private set
        {
            _collisionBounds = value;

            if (_collisionBounds != null)
            {
                var poly = new PolygonShape();
                poly.SetAsBox(_collisionBounds.Value);

                CollisionPolygon = poly;
            }
        }
    }

    private Box2? _collisionBounds = null;

    /// <summary>
    /// The polygon shape associated with the prototype CollisionBounds (if set)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public PolygonShape? CollisionPolygon { get; private set; } = null;

    /// <summary>
    /// Governs how the local rotation of the constructed entity will be set
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public RpdRotation Rotation { get; private set; } = RpdRotation.User;
}

public enum RpdMode : byte
{
    Invalid,
    Deconstruct,
    ConstructObject,
}

// These are to be replaced with more flexible 'RulesRule' at a later time
public enum RpdConstructionRule : byte
{
    MustBuildOnSubfloor,  // Can only be built on exposed subfloor (e.g. catwalks on lattice or hull plating)
    IsWindow,       // The entity is a window and can be built on grilles
    IsWall
}

public enum RpdRotation : byte
{
    Fixed,      // The entity has a local rotation of zero
    Camera,     // The rotation of the entity matches the local player camera
    User,       // The entity can be rotated by the local player prior to placement
}
