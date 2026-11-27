using Content.Shared.ADT.Utility;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Provides basic visuals for hitscan weapons - works with <see cref="HitscanBasicRaycastComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicVisualsComponent : Component
{
// ADT-Tweak Start
//     /// <summary>
//     /// The muzzle flash from the hitscan weapon.
//     /// </summary>
// ADT-Tweak End
    [DataField]
    public SpriteSpecifier? MuzzleFlash;

    [DataField]
    public SpriteSpecifier? TravelFlash;

    [DataField]
    public SpriteSpecifier? ImpactFlash;

    /// <summary>
// ADT-Tweak Start
//     /// The "travel" sprite, this gets repeated until it hits the target.
// ADT-Tweak End
    /// Flying bullet sprite shown on the client during the hitscan (Starlight Shooting 2.0).
    /// </summary>
    [DataField]
// ADT-Tweak Start
//     public SpriteSpecifier? TravelFlash;
// ADT-Tweak End
    public ExtendedSpriteSpecifier? Bullet;

    /// <summary>
// ADT-Tweak Start
//     /// The sprite that gets shown on the impact of the laser.
// ADT-Tweak End
    /// Display speed for the client bullet animation.
    /// </summary>
    [DataField]
// ADT-Tweak Start
//     public SpriteSpecifier? ImpactFlash;
// ADT-Tweak End
    public float Speed = 315f;

    /// <summary>
// ADT-Tweak Start
//     /// ADT BSA
//     /// Client-side lifetime of hitscan visual effect in seconds.
// ADT-Tweak End
    /// ADT BSA: client lifetime for legacy sprite-list effects.
    /// </summary>
    [DataField("effectLifetime")]
    public float EffectLifetime = 0.48f;

    /// <summary>
    /// How long the muzzle, the beam, and the impact stay up after they finish playing, in seconds.
    /// </summary>
    [DataField]
    public float MuzzleLinger = 0.6f;

    [DataField]
    public float TravelLinger = 0.8f;

    [DataField]
    public float ImpactLinger = 1.3f;

    /// <summary>
    /// Scale of the beam at the moment it leaves the muzzle. It grows to the full shot distance.
    /// </summary>
    [DataField]
    public float BeamStartScale = 0.05f;

    /// <summary>
    /// Vertical scale of the muzzle, beam, and impact sprites.
    /// </summary>
    [DataField]
    public float FlashThickness = 1f;

    /// <summary>
    /// Spawned on impact. Null skips it.
    /// </summary>
    [DataField]
    public EntProtoId? ImpactSparks = "ImpactSparksEffect";

    /// <summary>
    /// Hole left on a non-mob target. Null skips it.
    /// </summary>
    [DataField]
    public EntProtoId? BulletHole = "BulletHoleEffect";

    /// <summary>
    /// How far the hole sits along the shot from the impact, in tiles.
    /// </summary>
    [DataField]
    public float BulletHoleOffset = 0.15f;
}
