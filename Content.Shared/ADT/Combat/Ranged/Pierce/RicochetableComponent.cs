namespace Content.Shared.ADT.Combat.Ranged.Pierce;

/// <summary>
/// Entities with this component can ricochet hitscan kinetic rounds.
/// </summary>
[RegisterComponent]
public sealed partial class RicochetableComponent : Component
{
    [DataField("chance")]
    public float Chance = 1f;
}
