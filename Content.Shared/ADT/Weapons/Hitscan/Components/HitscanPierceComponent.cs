using Content.Shared.ADT.Combat.Ranged.Pierce;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Hitscan.Components;

/// <summary>
/// Entities with this can pierce through things which have PierceableComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanPierceComponent : Component
{
    [DataField]
    public float Chance = 0.1f;

    /// <summary>
    /// Max angle jitter in radians when piercing ("swim"). Starlight default ±0.1.
    /// </summary>
    [DataField]
    public float Deviation = 0.1f;

    [DataField]
    public PierceLevel PierceLevel = PierceLevel.Flesh;
}
