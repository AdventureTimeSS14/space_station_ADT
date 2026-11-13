using System.Numerics;

namespace Content.Client.ADT.Particles;

/// <summary>
/// Pre-sampled lifetime curves shared by every emitter using the same prototype.
/// The small tables trade imperceptible precision for cheap, predictable per-particle lookups.
/// </summary>
internal sealed class ParticleCurveCache
{
    public const int Resolution = 128;

    public float[]? Alpha { get; init; }
    public float[]? Size { get; init; }
    public float[]? Speed { get; init; }
    public float[]? Emission { get; init; }
    public Color[]? Colors { get; init; }
    public Vector2[]? Force { get; init; }
    public Vector2[]? Velocity { get; init; }

    public static float Sample(float[] samples, float time)
    {
        var position = Math.Clamp(time, 0f, 1f) * (samples.Length - 1);
        var index = (int)position;
        var next = Math.Min(index + 1, samples.Length - 1);
        return MathHelper.Lerp(samples[index], samples[next], position - index);
    }

    public static float Sample(float[] samples, int index, float amount)
    {
        index = Math.Min(index, samples.Length - 1);
        var next = Math.Min(index + 1, samples.Length - 1);
        return MathHelper.Lerp(samples[index], samples[next], amount);
    }

    public static Color Sample(Color[] samples, float time)
    {
        var position = Math.Clamp(time, 0f, 1f) * (samples.Length - 1);
        var index = (int)position;
        var next = Math.Min(index + 1, samples.Length - 1);
        return Color.InterpolateBetween(samples[index], samples[next], position - index);
    }

    public static Color Sample(Color[] samples, int index, float amount)
    {
        index = Math.Min(index, samples.Length - 1);
        var next = Math.Min(index + 1, samples.Length - 1);
        return Color.InterpolateBetween(samples[index], samples[next], amount);
    }

    public static Vector2 Sample(Vector2[] samples, float time)
    {
        var position = Math.Clamp(time, 0f, 1f) * (samples.Length - 1);
        var index = (int)position;
        var next = Math.Min(index + 1, samples.Length - 1);
        return Vector2.Lerp(samples[index], samples[next], position - index);
    }

    public static Vector2 Sample(Vector2[] samples, int index, float amount)
    {
        index = Math.Min(index, samples.Length - 1);
        var next = Math.Min(index + 1, samples.Length - 1);
        return Vector2.Lerp(samples[index], samples[next], amount);
    }
}
