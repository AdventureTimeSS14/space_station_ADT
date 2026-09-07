using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._OH.Stratogem;

/// <summary>
/// Marks an entity as an active stratogem strike zone.
/// While alive, it periodically calls in an explosion at a random point within <see cref="Radius"/>
/// of its own position, until <see cref="ImpactCount"/> impacts have landed.
/// Spawned by throwable stratogem shells via SpawnOnTrigger.
/// </summary>
[RegisterComponent]
public sealed partial class StratogemStrikeComponent : Component
{
    /// <summary>
    /// The explosion prototype used for every impact.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ExplosionPrototype> ExplosionType = default!;

    [DataField]
    public float TotalIntensity = 50f;

    [DataField]
    public float IntensitySlope = 5f;

    [DataField]
    public float MaxIntensity = 50f;

    /// <summary>
    /// Radius around the strike zone's origin in which impacts may land.
    /// </summary>
    [DataField]
    public float Radius = 5f;

    /// <summary>
    /// Time between individual impacts.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Total number of impacts before the strike ends and the zone deletes itself.
    /// </summary>
    [DataField]
    public int ImpactCount = 5;

    /// <summary>
    /// Sound played on each impact.
    /// </summary>
    [DataField]
    public SoundSpecifier? ImpactSound;

    /// <summary>
    /// If a <see cref="StratogemJammerComponent"/> is within its range of this zone's origin,
    /// the strike is cancelled entirely instead of firing any impacts.
    /// </summary>
    [DataField]
    public bool CheckJammers = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public int ImpactsDone;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextImpact;
}
