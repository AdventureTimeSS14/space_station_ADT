namespace Content.Shared.ADT.Objectives.Components;

[RegisterComponent]
public sealed partial class BlobCaptureConditionComponent : Component
{
    [DataField]
    public int Target = 0;

    public const int VictoryThreshold = 800;

    public const int CriticalThreshold = 400;
}