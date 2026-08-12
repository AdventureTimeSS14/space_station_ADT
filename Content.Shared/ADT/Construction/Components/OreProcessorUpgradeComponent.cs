namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class OreProcessorUpgradeComponent : Component
{
    [DataField]
    public float OutputMultiplier = 1f;

    [DataField]
    public float PointsMultiplier = 1f;
}
