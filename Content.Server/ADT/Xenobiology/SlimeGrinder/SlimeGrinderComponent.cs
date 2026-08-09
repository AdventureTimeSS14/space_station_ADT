using Content.Shared.ADT.Construction.Prototypes;
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

    // ADT-Tweak-Start: машинные части с тирами (порт Orion PR #385)
    [DataField]
    public ProtoId<MachinePartPrototype> ServoPart = "Servo";

    /// <summary>
    /// Множитель количества экстрактов (Т1 = x1, Т2 = x2, Т3 = x3, Т4 = x4).
    /// </summary>
    [DataField]
    public float ExtractMultiplier = 1f;

    /// <summary>
    /// Множитель скорости переработки (меньше = быстрее).
    /// </summary>
    [DataField]
    public float WorkTimeMultiplier = 1f;
    // ADT-Tweak-End

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
