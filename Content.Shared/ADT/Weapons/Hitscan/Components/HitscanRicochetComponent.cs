using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities that have this will be able to ricochet off of things which have RicochetableComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanRicochetComponent : Component
{
    [DataField]
    public float Chance;
}
