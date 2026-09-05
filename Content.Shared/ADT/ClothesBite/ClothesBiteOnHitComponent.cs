using Content.Shared.FixedPoint;

namespace Content.Shared.ADT.ClothesBite;

/// <summary>
/// When this entity melee-attacks a mob that wears clothing it can digest, it draws <see cref="Amount"/>
/// from one such garment's food solution (chosen at random) into its stomach, as if it had taken a bite.
/// Used by mothroaches so they can feed off worn clothing by attacking.
/// </summary>
[RegisterComponent]
public sealed partial class ClothesBiteOnHitComponent : Component
{
    /// <summary>
    /// How much of the garment's food solution is drawn per attack.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = FixedPoint2.New(2);
}
