namespace Content.Shared.Traits;

public sealed partial class TraitCategoryPrototype
{
    [DataField]
    public int Priority;

    [DataField]
    public int? MaxTraits;

    [DataField]
    public Color AccentColor = Color.FromHex("#4a9eff");

    [DataField]
    public bool DefaultExpanded = true;
}
