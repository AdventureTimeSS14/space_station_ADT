namespace Content.Shared.ADT.EatToGrow;

[RegisterComponent]
public sealed partial class EatToGrowComponent : Component
{
    [DataField]
    public float Growth = 0.1f;

    [DataField]
    public float MaxGrowth = 5f;

    [DataField]
    public bool ShrinkOnDeath = true;

    [ViewVariables]
    public float CurrentScale = 1f;
}
