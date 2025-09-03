using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour.Components;

/// <summary>
/// Component for characters who can devour others
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PredatorComponent : Component
{
    /// <summary>
    /// Whether this predator can digest living prey
    /// </summary>
    [DataField("canDigestLiving")]
    public bool CanDigestLiving = true;

    /// <summary>
    /// Whether this predator can digest dead prey
    /// </summary>
    [DataField("canDigestDead")]
    public bool CanDigestDead = true;

    /// <summary>
    /// Base digestion speed multiplier
    /// </summary>
    [DataField("digestionSpeedMultiplier")]
    public float DigestionSpeedMultiplier = 1.0f;

    /// <summary>
    /// Whether this predator produces scat
    /// </summary>
    [DataField("producesScat")]
    public bool ProducesScat = true;
}
