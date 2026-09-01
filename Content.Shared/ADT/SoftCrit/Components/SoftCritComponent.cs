using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.SoftCrit.Components;

/// <summary>
///     SS13-style soft crit: the mob is heavily damaged but still alive and
///     can only slowly crawl. Applied when total damage exceeds the threshold.
/// </summary>
[RegisterComponent]
public sealed partial class SoftCritComponent : Component
{
    /// <summary>
    ///     Total damage at which the mob enters soft crit.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 60;

    /// <summary>
    ///     Walk speed modifier while in soft crit.
    /// </summary>
    [DataField]
    public float WalkModifier = 0.5f;

    /// <summary>
    ///     Sprint speed modifier while in soft crit (running is barely possible, tg-style crawl).
    /// </summary>
    [DataField]
    public float SprintModifier = 0.25f;
}
