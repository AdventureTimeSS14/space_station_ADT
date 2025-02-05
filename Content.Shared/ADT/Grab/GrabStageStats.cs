using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Grab;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class GrabStageStats
{
    /// <summary>
    /// How much hands are required to be able to grab something at this stage.
    /// </summary>
    [DataField]
    public int RequiredHands = 1;

    /// <summary>
    /// How many doafters are required to escape from this stage.
    /// </summary>
    [DataField]
    public int DoaftersToEscape = 1;

    /// <summary>
    /// Movement speed modifier for the puller when they are at this stage.
    /// </summary>
    [DataField]
    public float MovementSpeedModifier = 0.95f;

    /// <summary>
    /// Grabbed entity escape attempt duration for this stage.
    /// </summary>
    [DataField]
    public float EscapeAttemptTime = 1f;

    [DataField]
    public float SetStageTime = 1.5f;
}
