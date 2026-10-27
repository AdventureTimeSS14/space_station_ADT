using System.Linq;
using System.Numerics;

namespace Content.Client.ADT.Drawing.Input;

public sealed class PressureCurve
{
    private Vector2[] _points;

    public PressureCurve(params Vector2[] points)
    {
        _points = Normalize(points);
    }

    public IReadOnlyList<Vector2> Points => _points;

    public static PressureCurve Soft => FromGamma(0.6f);

    public static PressureCurve Linear => new(new Vector2(0, 0), new Vector2(1, 1));

    public static PressureCurve Hard => FromGamma(1.7f);

    public static PressureCurve FromGamma(float gamma, int samples = 9)
    {
        var points = new Vector2[samples];
        for (var i = 0; i < samples; i++)
        {
            var x = i / (float) (samples - 1);
            points[i] = new Vector2(x, MathF.Pow(x, gamma));
        }

        return new PressureCurve(points);
    }

    public void SetPoints(params Vector2[] points)
    {
        _points = Normalize(points);
    }

    public float Evaluate(float pressure)
    {
        pressure = Math.Clamp(pressure, 0f, 1f);

        if (pressure <= _points[0].X)
            return _points[0].Y;

        for (var i = 1; i < _points.Length; i++)
        {
            var b = _points[i];
            if (pressure > b.X)
                continue;

            var a = _points[i - 1];
            var t = b.X - a.X <= float.Epsilon ? 1f : (pressure - a.X) / (b.X - a.X);
            return Math.Clamp(a.Y + (b.Y - a.Y) * t, 0f, 1f);
        }

        return _points[^1].Y;
    }

    private static Vector2[] Normalize(Vector2[] points)
    {
        if (points.Length == 0)
            return new[] { Vector2.Zero, Vector2.One };

        var result = points
            .Select(p => Vector2.Clamp(p, Vector2.Zero, Vector2.One))
            .OrderBy(p => p.X)
            .ToList();

        if (result[0].X > 0f)
            result.Insert(0, new Vector2(0, result[0].Y));
        if (result[^1].X < 1f)
            result.Add(new Vector2(1, result[^1].Y));

        return result.ToArray();
    }
}
