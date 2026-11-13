using System.Numerics;
using Content.Shared.ADT.Particles;

namespace Content.Client.ADT.Particles;

/// <summary>
/// Bakes prototype curves into lookup tables and provides the static keyframe samplers used for baking
/// and by external visual systems.
/// </summary>
public sealed partial class ParticleSystem
{
    #region =^..^= Curve Samplers =^..^=

    private ParticleCurveCache GetCurveCache(ParticleEffectPrototype prototype)
    {
        if (_curveCache.TryGetValue(prototype.ID, out var cached))
            return cached;

        cached = new ParticleCurveCache
        {
            Alpha = BakeCurve(prototype.AlphaOverLifetime),
            Size = BakeCurve(prototype.SizeOverLifetime),
            Speed = BakeCurve(prototype.SpeedOverLifetime),
            Emission = BakeCurve(prototype.EmissionOverTime),
            Colors = BakeColorCurve(prototype.ColorOverLifetime),
            Force = BakeVectorCurve(prototype.ForceOverLifetime),
            Velocity = BakeVectorCurve(prototype.VelocityOverLifetime),
        };
        _curveCache.Add(prototype.ID, cached);
        return cached;
    }

    private static float[]? BakeCurve(List<ParticleCurveKey> curve)
    {
        if (curve.Count == 0)
            return null;
        if (curve.Count == 1)
            return [curve[0].Value];

        var samples = new float[ParticleCurveCache.Resolution];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = SampleCurve(curve, i / (float)(samples.Length - 1));
        return samples;
    }

    private static Color[]? BakeColorCurve(List<ColorCurveKey> curve)
    {
        if (curve.Count == 0)
            return null;
        if (curve.Count == 1)
            return [curve[0].Color];

        var samples = new Color[ParticleCurveCache.Resolution];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = SampleColorCurve(curve, i / (float)(samples.Length - 1));
        return samples;
    }

    private static Vector2[]? BakeVectorCurve(List<Vector2CurveKey> curve)
    {
        switch (curve.Count)
        {
            case 0:
                return null;
            case 1:
                return [curve[0].Value];
        }

        var samples = new Vector2[ParticleCurveCache.Resolution];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = SampleVector2Curve(curve, i / (float)(samples.Length - 1));
        return samples;
    }

    // ᓚᘏᗢ <( math scares me
    public static float SampleCurve(List<ParticleCurveKey> curve, float t)
    {
        if (curve.Count == 0)
            return 1f;
        if (curve.Count == 1)
            return curve[0].Value;

        ParticleCurveKey? prev = null, next = null;
        foreach (var key in curve)
        {
            if (key.Time <= t)
                prev = key;
            else
            {
                next = key;
                break;
            }
        }
        if (prev == null)
            return curve[0].Value;
        if (next == null)
            return prev.Value;

        var span = next.Time - prev.Time;
        if (span <= 0f)
            return prev.Value;
        return prev.Value + (next.Value - prev.Value) * ((t - prev.Time) / span);
    }

    public static Color SampleColorCurve(List<ColorCurveKey> curve, float t)
    {
        if (curve.Count == 0)
            return Color.White;
        if (curve.Count == 1)
            return curve[0].Color;

        ColorCurveKey? prev = null, next = null;
        foreach (var key in curve)
        {
            if (key.Time <= t)
                prev = key;
            else
            {
                next = key;
                break;
            }
        }
        if (prev == null)
            return curve[0].Color;
        if (next == null)
            return prev.Color;

        var span = next.Time - prev.Time;
        if (span <= 0f)
            return prev.Color;
        return Color.InterpolateBetween(prev.Color, next.Color, (t - prev.Time) / span);
    }

    public static Vector2 SampleVector2Curve(List<Vector2CurveKey> curve, float t)
    {
        if (curve.Count == 0)
            return Vector2.Zero;
        if (curve.Count == 1)
            return curve[0].Value;

        Vector2CurveKey? prev = null, next = null;
        foreach (var key in curve)
        {
            if (key.Time <= t)
                prev = key;
            else
            {
                next = key;
                break;
            }
        }
        if (prev == null)
            return curve[0].Value;
        if (next == null)
            return prev.Value;

        var span = next.Time - prev.Time;
        if (span <= 0f)
            return prev.Value;
        return Vector2.Lerp(prev.Value, next.Value, (t - prev.Time) / span);
    }

    #endregion

    #region =^..^= Value Noise =^..^=

    private const int NoiseTableSize = 64;
    private static readonly float[] NoiseTable = CreateNoiseTable();

    /// <summary>
    /// A simple 2D value noise function for particle turbulence. Not Perlin or Simplex, just a grid of random values with smooth interpolation.
    /// </summary>
    private static float ValueNoise(float x, float y)
    {
        var ix = (int)MathF.Floor(x);
        var iy = (int)MathF.Floor(y);
        var fx = x - ix;
        var fy = y - iy;

        // Smooth interpolation
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);

        var a = SampleNoiseTable(ix,     iy);
        var b = SampleNoiseTable(ix + 1, iy);
        var c = SampleNoiseTable(ix,     iy + 1);
        var d = SampleNoiseTable(ix + 1, iy + 1);

        // ᓚᘏᗢ <( maths scare me what do these letters mean
        return a + (b - a) * fx + (c - a) * fy + (d - b - c + a) * fx * fy;
    }

    private static float SampleNoiseTable(int x, int y)
        => NoiseTable[(x & (NoiseTableSize - 1)) + (y & (NoiseTableSize - 1)) * NoiseTableSize];

    private static float[] CreateNoiseTable()
    {
        var table = new float[NoiseTableSize * NoiseTableSize];
        for (var y = 0; y < NoiseTableSize; y++)
        {
            for (var x = 0; x < NoiseTableSize; x++)
                table[x + y * NoiseTableSize] = NoiseHash(x, y);
        }
        return table;
    }

    // ᓚᘏᗢ <( random bullshit go (once, when the table is made, instead of per particle forever)
    private static float NoiseHash(int x, int y)
    {
        var n = x + y * 57;
        n = (n << 13) ^ n;
        return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
    }

    #endregion

    /// <summary>Rotates a vector by the given radians.</summary>
    private static Vector2 RotateVector(Vector2 vector, float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Vector2(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos);
    }
}
