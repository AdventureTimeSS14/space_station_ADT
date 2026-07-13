using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

/// <summary>
/// Traits category with general settings. Allows you to limit the number of taken traits in one category
/// </summary>
[Prototype]
public sealed partial class TraitCategoryPrototype : IPrototype
{
    public const string Default = "Default";

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Name of the trait category displayed in the UI
    /// </summary>
    [DataField(required: true)] // ADT-Tweak new Traits
    public LocId Name { get; private set; } = string.Empty;

    // ADT-Tweak start new Traits
    /// <summary>
    /// Display order priority. Lower values appear first.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Maximum number of traits that can be selected from this category.
    /// Null means unlimited (only global limit applies).
    /// </summary>
    [DataField]
    public int? MaxTraits;

    /// <summary>
    /// Maximum trait points that can be spent in this category.
    /// Null means unlimited (only global limit applies).
    /// </summary>
    [DataField]
    public int? MaxPoints;

    /// <summary>
    /// Color hex for the category header accent.
    /// </summary>
    [DataField]
    public Color AccentColor = Color.FromHex("#4a9eff");

    /// <summary>
    /// Whether this category starts expanded or collapsed.
    /// </summary>
    [DataField]
    public bool DefaultExpanded = true; 
}
// ADT-Tweak new Traits - End
