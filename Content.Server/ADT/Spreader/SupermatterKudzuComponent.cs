namespace Content.Server.ADT.Spreader;

/// <summary>
/// Handles entities that spread out when they reach the relevant growth level.
/// </summary>
[RegisterComponent]
public sealed partial class SupermatterKudzuComponent : Component
{
    /// <summary>
    /// At level 3 spreading can occur; prior to that we have a chance of increasing our growth level and changing our sprite.
    /// </summary>
    [DataField]
    public int GrowthLevel = 1;

    /// <summary>
    /// Chance to spread whenever an edge spread is possible.
    /// </summary>
    [DataField]
    public float SpreadChance = 1f;

    [DataField]
    public float GrowthTickChance = 1f;

    /// <summary>
    /// number of sprite variations for kudzu
    /// </summary>
    [DataField]
    public int SpriteVariants = 3;
}
