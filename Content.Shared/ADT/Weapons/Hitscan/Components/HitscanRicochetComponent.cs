using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Hitscan.Components;

/// <summary>
/// Entities with this can ricochet off of things which have RicochetableComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanRicochetComponent : Component
{
    [DataField]
    public float Chance;
}
