namespace Content.Shared.ADT.Combat.Ranged.Pierce;

/// <summary>
/// Entities with this component can block hitscan pierce based on <see cref="Level"/>.
/// </summary>
[RegisterComponent]
public sealed partial class PierceableComponent : Component
{
    [DataField]
    public PierceLevel Level = PierceLevel.Metal;
}
