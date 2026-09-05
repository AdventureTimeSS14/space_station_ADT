using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities with this component ignite flammable targets on hit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanIgniteComponent : Component
{
    [DataField]
    public float FireStacks = 0.25f;
}
