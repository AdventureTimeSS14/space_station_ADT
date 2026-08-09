namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class GrillUpgradeComponent : Component
{
    [DataField]
    public float BasePower = 2400f;

    [DataField]
    public float PowerMultiplier = 1f;
}
