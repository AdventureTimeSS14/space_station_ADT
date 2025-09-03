using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour.Components;

/// <summary>
/// Component for characters who can be devoured by predators
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreyComponent : Component
{
    /// <summary>
    /// Whether this prey can be digested while alive
    /// </summary>
    [DataField("digestibleWhileAlive")]
    public bool DigestibleWhileAlive = true;

    /// <summary>
    /// Whether this prey can be digested while dead
    /// </summary>
    [DataField("digestibleWhileDead")]
    public bool DigestibleWhileDead = true;

    /// <summary>
    /// Resistance to digestion (higher = slower digestion)
    /// </summary>
    [DataField("digestionResistance")]
    public float DigestionResistance = 1.0f;

    /// <summary>
    /// Whether this prey can be devoured by non-predators
    /// </summary>
    [DataField("devourableByNonPredators")]
    public bool DevourableByNonPredators = false;

    /// <summary>
    /// Whether this prey produces nutrients when digested
    /// </summary>
    [DataField("producesNutrients")]
    public bool ProducesNutrients = true;

    /// <summary>
    /// Whether this prey consents to being eaten unwillingly.
    /// If false, predators cannot force-devour this mob via the action.
    /// Defaults to false.
    /// </summary>
    [DataField("allowUnwillingDevour")]
    public bool AllowUnwillingDevour = false;

    /// <summary>
    /// If true, cannot be digested at all (alive or dead).
    /// </summary>
    [DataField("undigestible")]
    public bool Undigestible = false;
}
