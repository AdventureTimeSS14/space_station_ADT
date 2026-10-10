using System.Numerics;
using Content.Shared.ADT.Weapons.Hitscan.Events;
using Content.Shared.Damage;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Hitscan.Events;

/// <summary>
/// Raised on the hitscan entity when "fired". This could be from reflections or from the gun. This is the catalyst that
/// other systems will listen for to actually shoot the gun.
/// </summary>
[ByRefEvent]
public record struct HitscanTraceEvent
{
    /// <summary>
    /// Location the hitscan was fired from.
    /// </summary>
    public EntityCoordinates FromCoordinates;

    /// <summary>
    /// Direction that the ray was fired towards.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// Gun that was fired - this will always be the original weapon even if reflected.
    /// </summary>
    public EntityUid Gun;

    /// <summary>
    /// Player who shot the gun, if null the gun was fired by itself.
    /// Keep the original shooter across pierce/ricochet/reflect so they stay ignored by raycasts.
    /// </summary>
    public EntityUid? Shooter;

    /// <summary>
    /// Extra entity to ignore for this leg (e.g. the body/wall just pierced or bounced from).
    /// </summary>
    public EntityUid? IgnoredEntity;

    /// <summary>
    /// Target that was being aimed at (Not necessarily hit).
    /// </summary>
    public EntityUid? Target;

    /// <summary>
    /// ADT hitscan #3142: aim destination used for distance-based MobMover hit chance.
    /// </summary>
    public EntityCoordinates ToCoordinates;

    /// <summary>
    /// Aggregated visual segments for bounce/pierce/reflect (Starlight Shooting 2.0).
    /// </summary>
    public List<HitscanTrace>? OutputTrace;
}

/// <summary>
/// All data known data for when a hitscan is actually fired.
/// </summary>
public record struct HitscanRaycastFiredData
{
    /// <summary>
    /// Direction that the ray was fired towards.
    /// </summary>
    public Vector2 ShotDirection;

    /// <summary>
    /// The entity that got hit, if null the raycast didn't hit anyone.
    /// </summary>
    public EntityUid? HitEntity;

    /// <summary>
    /// Gun that fired the raycast.
    /// </summary>
    public EntityUid Gun;

    /// <summary>
    /// Player who shot the gun, if null the gun was fired by itself.
    /// </summary>
    public EntityUid? Shooter;

    /// <summary>
    /// World position of the impact, if null the raycast didn't hit anyone.
    /// </summary>
    public Vector2? HitPosition;

    /// <summary>
    /// Aggregated visual segments for the full hitscan chain.
    /// </summary>
    public List<HitscanTrace>? OutputTrace;
}

/// <summary>
/// Try to hit the targeted entity with a hitscan laser. Stuff like the reflection system should listen for this and
/// cancel the event if the laser was reflected.
/// </summary>
[ByRefEvent]
public struct AttemptHitscanRaycastFiredEvent
{
    /// <summary>
    /// Data for the hitscan that was fired.
    /// </summary>
    public HitscanRaycastFiredData Data;

    /// <summary>
    /// Set to true the hitscan is cancelled (e.g. due to reflection).
    /// Cancelled hitscans should not apply damage or trigger follow-up effects.
    /// </summary>
    public bool Cancelled;
}

/// <summary>
/// Results of a hitscan raycast and will be raised on the raycast entity on itself. Stuff like the damage system should
/// listen for this. At this point we KNOW the laser hit the entity.
/// </summary>
[ByRefEvent]
public struct HitscanRaycastFiredEvent
{
    /// <summary>
    /// Data for the hitscan that was fired.
    /// </summary>
    public HitscanRaycastFiredData Data;
}

[ByRefEvent]
public record struct HitscanDamageDealtEvent
{
    /// <summary>
    /// Target that was dealt damage.
    /// </summary>
    public EntityUid Target;

    /// <summary>
    /// The amount of damage that the target was dealt.
    /// </summary>
    public DamageSpecifier DamageDealt;

    /// <summary>
    /// ADT: raycast data for blood spray / follow-up effects (Starlight Shooting 2.0).
    /// </summary>
    public HitscanRaycastFiredData Data;
}
