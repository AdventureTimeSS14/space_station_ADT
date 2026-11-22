using System.Numerics;
using Robust.Shared.Map;

namespace Content.Client.ADT.Particles;

/// <summary>
/// A single live particle. Class so it can be pooled in place.
/// </summary>
public sealed class ParticleData
{
    /// <summary>
    /// Screen-relative simulation offset from the particle origin, X = right, Y = up.
    /// Rendering converts this into the coordinate parent's local space before applying its world transform.
    /// </summary>
    public Vector2 LocalOffset;

    /// <summary>
    /// Grid-relative position at spawn time, used for grid- and map-space particles.
    /// It is resolved to map space when drawn so particles travel with moving grids.
    /// Unused for emitter-local particles.
    /// </summary>
    public EntityCoordinates SpawnCoordinates;

    /// <summary>Map position captured at spawn for map-space simulation.</summary>
    public Vector2 SpawnMapPosition;

    public Vector2 Velocity;        // current movement vector in screen-space units/sec
    public float Age;               // seconds this particle has been alive
    public float Lifetime;          // total lifespan in seconds
    public float SpawnSpeed;        // speed magnitude at spawn, used by SpeedOverLifetime
    public float SpawnIntensity;    // emitter intensity captured at spawn, used to scale rendered size
    public float Rotation;          // current rotation in radians
    public float RotationSpeed;     // spin rate in radians per second
    public int CurveIndex;          // cached lookup-table position for the current age
    public float CurveLerp;         // interpolation amount toward the next lookup-table entry

    /// <summary>Size multiplier baked in at spawn from SizeVariance.</summary>
    public float SizeMultiplier = 1f;

    /// <summary>Noise seed so each particle gets different turbulence.</summary>
    public Vector2 NoiseOffset;
    public int ParticlesIndex;

    public float AgeRatio => Lifetime > 0f ? Math.Clamp(Age / Lifetime, 0f, 1f) : 1f;

    public void UpdateCurveSample()
    {
        var position = AgeRatio * (ParticleCurveCache.Resolution - 1);
        CurveIndex = (int)position;
        CurveLerp = position - CurveIndex;
    }

    public void Reset()
    {
        LocalOffset = default;
        SpawnCoordinates = default;
        SpawnMapPosition = default;
        Velocity = default;
        Age = 0f;
        Lifetime = 1f;
        SpawnSpeed = 0f;
        SpawnIntensity = 1f;
        Rotation = 0f;
        RotationSpeed = 0f;
        CurveIndex = 0;
        CurveLerp = 0f;
        SizeMultiplier = 1f;
        NoiseOffset = default;
        ParticlesIndex = 0;
    }
}
