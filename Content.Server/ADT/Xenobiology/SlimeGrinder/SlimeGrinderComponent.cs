using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.SlimeGrinder;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SlimeGrinderComponent : Component
{
    /// <summary>
    /// This gets set for each mob it processes.
    /// When it hits 0, spit out extract.
    /// </summary>
    [ViewVariables]
    public float ProcessingTimer = default;

    /// <summary>
    /// The entity being ground.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntProtoId, float> YieldQueue = new();

    /// <summary>
    /// The time it takes to process a mob, per mass.
    /// </summary>
    [DataField]
    public float ProcessingTimePerUnitMass = 0.1f;

    [DataField]
    public float ExtractMultiplier = 1f;

    [DataField]
    public float WorkTimeMultiplier = 1f;

    [DataField]
    public SoundSpecifier GrindSound = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");

    [DataField]
    public float AutoFeedRange = 2f;

    [DataField]
    public TimeSpan ScanInterval = TimeSpan.FromSeconds(1);

    [DataField]
    [AutoPausedField]
    public TimeSpan NextScan = TimeSpan.Zero;
}
