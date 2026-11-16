using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Hitscan.Components;

/// <summary>
/// Starlight Shooting 3.3: damage scales down with distance travelled along the hitscan trace.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanDamageFalloffComponent : Component
{
    [DataField]
    public float FullDamageDistance = 2f;

    [DataField]
    public float MinDamageDistance = 8f;

    [DataField]
    public float MinMultiplier = 0.25f;
}
