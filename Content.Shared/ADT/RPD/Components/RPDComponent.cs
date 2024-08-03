using Content.Shared.ADT.RPD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.RPD.Components;

/// <summary>
/// Main component for the RPD
/// Optionally uses LimitedChargesComponent.
/// Charges can be refilled with RPD ammo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RPDSystem))]
public sealed partial class RPDComponent : Component
{
    /// <summary>
    /// List of RPD prototypes that the device comes loaded with
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RPDPrototype>> AvailablePrototypes { get; set; } = new();

    /// <summary>
    /// Sound that plays when a RPD operation successfully completes
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessSound { get; set; } = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// The ProtoId of the currently selected RPD prototype
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RPDPrototype> ProtoId { get; set; } = "Invalid";

    /// <summary>
    /// A cached copy of currently selected RPD prototype
    /// </summary>
    /// <remarks>
    /// If the ProtoId is changed, make sure to update the CachedPrototype as well
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public RPDPrototype CachedPrototype { get; set; } = default!;

    /// <summary>
    /// The direction constructed entities will face upon spawning
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction ConstructionDirection
    {
        get
        {
            return _constructionDirection;
        }
        set
        {
            _constructionDirection = value;
            ConstructionTransform = new Transform(new(), _constructionDirection.ToAngle());
        }
    }

    private Direction _constructionDirection = Direction.South;

    /// <summary>
    /// Returns a rotated transform based on the specified ConstructionDirection
    /// </summary>
    /// <remarks>
    /// Contains no position data
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public Transform ConstructionTransform { get; private set; } = default!;
}
