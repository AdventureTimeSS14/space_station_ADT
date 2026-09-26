namespace Content.Shared.ADT.Drake;

[RegisterComponent]
public sealed partial class ADTDrakeSequenceComponent : Component
{
    [DataField]
    public List<ADTDrakeStep> Queue = new();
}

[DataDefinition]
public sealed partial class ADTDrakeStep
{
    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public ADTDrakeStepType Type;

    [DataField]
    public int Index;

    [DataField]
    public int Count;

    [DataField]
    public int Range;

    [DataField]
    public TimeSpan Duration;

    [DataField]
    public EntityUid? Target;
}

public enum ADTDrakeStepType : byte
{
    MassFireWave,
    MassFireEnd,
    FireCone,
    SetRecovery,
    LavaPool,
    EscapeEnrageMassFire,
    EscapeEnrageEnd,
    MeleeFollowUp,
}
