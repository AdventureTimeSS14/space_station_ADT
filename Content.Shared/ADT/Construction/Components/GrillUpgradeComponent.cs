namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class GrillUpgradeComponent : Component
{
    [DataField]
    public float Power = 2400f;

    [DataField]
    public float PowerMultiplier = 1f;
}
