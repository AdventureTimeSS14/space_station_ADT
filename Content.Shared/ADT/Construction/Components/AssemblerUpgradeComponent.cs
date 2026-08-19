namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent]
public sealed partial class AssemblerUpgradeComponent : Component
{
    [DataField]
    public float IngredientMultiplier = 1f;
}
