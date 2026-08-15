namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class ArtifactAnalyzerUpgradeComponent : Component
{
    [DataField]
    public float PointMultiplier = 1f;
}
