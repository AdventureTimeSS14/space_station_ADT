namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class ArtifactCrusherUpgradeComponent : Component
{
    [DataField]
    public float WorkTimeMultiplier = 1f;

    [DataField]
    public float OutputMultiplier = 1f;
}
