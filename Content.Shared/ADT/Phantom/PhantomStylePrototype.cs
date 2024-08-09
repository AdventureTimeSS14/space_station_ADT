using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Phantom;

/// <summary>
/// A prototype for phantom styles.
/// </summary>
[Prototype("phantomStyle")]
public sealed partial class PhantomStylePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Icon for radial menu
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// Name of the style
    /// </summary>
    [DataField("name")]
    public string? Name;

    #region Lists
    [DataField("lvl1")]
    public List<string> Lvl1Actions = new();

    [DataField("lvl2")]
    public List<string> Lvl2Actions = new();

    [DataField("lvl3")]
    public List<string> Lvl3Actions = new();

    [DataField("lvl4")]
    public List<string> Lvl4Actions = new();

    [DataField("lvl5")]
    public List<string> Lvl5Actions = new();

    [DataField("lvl6")]
    public List<string> Lvl6Actions = new();

    [DataField("lvl7")]
    public List<string> Lvl7Actions = new();
    #endregion
}
