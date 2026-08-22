using Content.Shared.ADT.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Provides basic visuals for hitscan weapons - works with <see cref="HitscanBasicRaycastComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicVisualsComponent : Component
{
    [DataField]
    public SpriteSpecifier? MuzzleFlash;

    [DataField]
    public SpriteSpecifier? TravelFlash;

    [DataField]
    public SpriteSpecifier? ImpactFlash;

    /// <summary>
    /// Flying bullet sprite shown on the client during the hitscan (Starlight Shooting 2.0).
    /// </summary>
    [DataField]
    public ExtendedSpriteSpecifier? Bullet;

    /// <summary>
    /// Display speed for the client bullet animation.
    /// </summary>
    [DataField]
    public float Speed = 315f;

    /// <summary>
    /// ADT BSA: client lifetime for legacy sprite-list effects.
    /// </summary>
    [DataField("effectLifetime")]
    public float EffectLifetime = 0.48f;
}
