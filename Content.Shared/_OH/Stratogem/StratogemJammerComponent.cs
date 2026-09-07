namespace Content.Shared._OH.Stratogem;

/// <summary>
/// Placed on machines (e.g. an "anti-stratogem" console) that deny stratogem strikes.
/// Any <see cref="StratogemStrikeComponent"/> zone whose origin is within <see cref="Range"/>
/// of this entity is cancelled before it fires any impacts.
/// </summary>
[RegisterComponent]
public sealed partial class StratogemJammerComponent : Component
{
    /// <summary>
    /// Radius in which stratogem strikes are denied.
    /// </summary>
    [DataField]
    public float Range = 12f;

    /// <summary>
    /// Whether the jammer is currently functioning (e.g. can be powered off/destroyed).
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
