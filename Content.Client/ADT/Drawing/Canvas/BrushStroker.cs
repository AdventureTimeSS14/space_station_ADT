using System.Numerics;
using Content.Client.ADT.Drawing.Input;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class BrushSettings
{
    public Color Color = Color.Black;
    public float Size = 12f;
    public float Hardness = 0.8f;
    public float Opacity = 1f;
    public float Spacing = 0.08f;
    public bool PressureAffectsSize = true;
    public bool PressureAffectsOpacity;
    public float MinSizeFraction = 0.05f;
    public float MinOpacityFraction = 0.05f;
    public bool Pixel;

    public int PixelSize => Math.Clamp((int) MathF.Round(Size), 1, PixelMaxSize);

    public const int PixelMaxSize = 16;

    public float DiameterAt(float pressure)
    {
        if (Pixel)
            return PixelSize;

        return PressureAffectsSize ? Size * Lerp(MinSizeFraction, 1f, pressure) : Size;
    }

    public float OpacityAt(float pressure)
    {
        if (Pixel)
            return Opacity;

        return PressureAffectsOpacity ? Opacity * Lerp(MinOpacityFraction, 1f, pressure) : Opacity;
    }

    public BrushSettings Clone()
    {
        return (BrushSettings) MemberwiseClone();
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
}

public readonly record struct StrokePoint(Vector2 Position, float Pressure);

public sealed class StrokeRecord
{
    public readonly BrushSettings Settings;
    public readonly bool Erase;
    public readonly List<StrokePoint> Points = new();

    public StrokeRecord(BrushSettings settings, bool erase)
    {
        Settings = settings;
        Erase = erase;
    }
}

public sealed class BrushStroker
{
    public const int MaxDabsPerSegment = 2048;
    public const int MaxPointsPerStroke = 65536;

    private const float CoordinateLimit = PaintSurface.MaxSize * 2f;

    private readonly PaintSurface _surface;

    private StrokeRecord? _stroke;
    private int _strokeTag;
    private BrushSettings _active = new();
    private Vector2 _lastPos;
    private float _lastPressure;
    private float _carry;
    private int _tailStart;
    private readonly List<float> _distances = new();

    public BrushStroker(PaintSurface surface, BrushSettings settings)
    {
        _surface = surface;
        Settings = settings;
    }

    public BrushSettings Settings { get; set; }

    public float TaperOutLength { get; set; }

    public bool Active => _stroke != null;

    private float Taper => _active.Pixel ? 0f : TaperOutLength;

    public event Action<StrokeRecord, int, int[]>? StrokeFinished;

    public event Action<StrokeRecord, int>? StrokeStarted;

    public event Action<StrokePoint>? PointAdded;

    public event Action<int>? StrokeEnded;

    public void End()
    {
        if (_stroke != null)
            Finish();
    }

    public void Handle(in PenSample sample, int tag)
    {
        var valid = TrySanitize(sample, out var point);

        switch (sample.Phase)
        {
            case PenPhase.Down:
                if (!valid)
                    return;

                if (_stroke != null)
                    Finish();

                _active = Settings.Clone();
                _stroke = new StrokeRecord(_active, sample.Tool == PenTool.Eraser);
                _strokeTag = tag;
                _tailStart = 0;
                _surface.BeginStroke(_active.Color, _stroke.Erase);
                _stroke.Points.Add(point);

                if (Taper > 0f)
                    RedrawTail();
                else
                    Start(point);

                StrokeStarted?.Invoke(_stroke, tag);
                break;

            case PenPhase.Move when _stroke != null:
                if (valid)
                    Append(point);
                break;

            case PenPhase.Up when _stroke != null:
                if (valid)
                    Append(point);

                Finish();
                break;
        }
    }

    public void Replay(StrokeRecord stroke, int clipTile)
    {
        if (_stroke != null || stroke.Points.Count == 0)
            return;

        _active = stroke.Settings;
        _surface.BeginStroke(_active.Color, stroke.Erase, clipTile);
        Trace(stroke.Points);
        _surface.EndStroke(record: false);
    }

    private void Append(StrokePoint point)
    {
        var stroke = _stroke!;
        if (stroke.Points.Count >= MaxPointsPerStroke)
            return;

        stroke.Points.Add(point);

        if (Taper > 0f)
            RedrawTail();
        else
            Segment(point.Position, point.Pressure);

        PointAdded?.Invoke(point);
    }

    private void Finish()
    {
        var stroke = _stroke!;
        _stroke = null;

        if (Taper > 0f)
        {
            if (stroke.Points.Count == 1)
                DabAt(stroke.Points[0].Position, stroke.Points[0].Pressure);
            else
                ApplyTaperOut(stroke.Points, TaperOutLength);
        }

        var touched = _surface.EndStroke(tag: _strokeTag);
        StrokeEnded?.Invoke(_strokeTag);
        if (touched.Length > 0)
            StrokeFinished?.Invoke(stroke, _strokeTag, touched);
    }

    private void RedrawTail()
    {
        var points = _stroke!.Points;
        var length = TaperOutLength;
        var last = points.Count - 1;

        var tailStart = last;
        var fromEnd = 0f;
        while (tailStart > 0)
        {
            var step = Vector2.Distance(points[tailStart - 1].Position, points[tailStart].Position);
            if (fromEnd + step >= length)
                break;

            fromEnd += step;
            tailStart--;
        }

        var from = Math.Max(0, Math.Min(_tailStart, tailStart) - 1);
        _tailStart = tailStart;

        _distances.Clear();
        var distance = 0f;
        for (var i = last; i >= from; i--)
        {
            if (i < last)
                distance += Vector2.Distance(points[i].Position, points[i + 1].Position);

            _distances.Add(distance);
        }

        var first = points[from];
        _lastPos = first.Position;
        _lastPressure = first.Pressure * TaperFactor(_distances[last - from], length);
        _carry = 0f;
        DabAt(_lastPos, _lastPressure);

        for (var i = from + 1; i <= last; i++)
        {
            var p = points[i];
            Segment(p.Position, p.Pressure * TaperFactor(_distances[last - i], length));
        }
    }

    private static void ApplyTaperOut(List<StrokePoint> points, float length)
    {
        var fromEnd = 0f;
        for (var i = points.Count - 1; i >= 0; i--)
        {
            if (i < points.Count - 1)
                fromEnd += Vector2.Distance(points[i].Position, points[i + 1].Position);

            if (fromEnd >= length)
                break;

            points[i] = points[i] with { Pressure = points[i].Pressure * TaperFactor(fromEnd, length) };
        }
    }

    private static float TaperFactor(float fromEnd, float length)
    {
        if (fromEnd >= length)
            return 1f;

        var t = fromEnd / length;
        return t * t * (3f - 2f * t) * 0.8f + 0.2f;
    }

    private void Trace(List<StrokePoint> points)
    {
        Start(points[0]);
        for (var i = 1; i < points.Count; i++)
        {
            Segment(points[i].Position, points[i].Pressure);
        }
    }

    private void Start(StrokePoint point)
    {
        _lastPos = point.Position;
        _lastPressure = point.Pressure;
        _carry = 0f;
        DabAt(point.Position, point.Pressure);
    }

    private void Segment(Vector2 to, float toPressure)
    {
        if (_active.Pixel)
        {
            PixelLine(_lastPos, to);
            _lastPos = to;
            _lastPressure = toPressure;
            return;
        }

        var from = _lastPos;
        var fromPressure = _lastPressure;
        var length = Vector2.Distance(from, to);

        if (length <= float.Epsilon || !float.IsFinite(length))
        {
            _lastPressure = toPressure;
            return;
        }

        var minStep = MathF.Max(0.35f, length / MaxDabsPerSegment);
        var travelled = -_carry;
        while (true)
        {
            var pressureHere = Lerp(fromPressure, toPressure, Math.Clamp(travelled / length, 0f, 1f));
            var step = _active.DiameterAt(pressureHere) * _active.Spacing;
            step = float.IsFinite(step) ? MathF.Max(minStep, step) : length + 1f;

            if (travelled + step > length)
            {
                _carry = MathF.Max(0f, length - travelled);
                break;
            }

            travelled += step;
            var t = travelled / length;
            DabAt(Vector2.Lerp(from, to, t), Lerp(fromPressure, toPressure, t));
        }

        _lastPos = to;
        _lastPressure = toPressure;
    }

    private void PixelLine(Vector2 from, Vector2 to)
    {
        var x0 = (int) MathF.Floor(from.X);
        var y0 = (int) MathF.Floor(from.Y);
        var x1 = (int) MathF.Floor(to.X);
        var y1 = (int) MathF.Floor(to.Y);
        var dx = Math.Abs(x1 - x0);
        var dy = -Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            PixelAt(x0, y0);
            if (x0 == x1 && y0 == y1)
                break;

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void PixelAt(int x, int y)
    {
        var size = _active.PixelSize;
        var offset = (size - 1) / 2;
        _surface.FillCells(x - offset, y - offset, size, _active.Opacity);
    }

    private void DabAt(Vector2 position, float pressure)
    {
        if (_active.Pixel)
        {
            PixelAt((int) MathF.Floor(position.X), (int) MathF.Floor(position.Y));
            return;
        }

        var radius = _active.DiameterAt(pressure) * 0.5f;
        _surface.Dab(position, radius, _active.Hardness, _active.OpacityAt(pressure));
    }

    private static bool TrySanitize(in PenSample sample, out StrokePoint point)
    {
        var pos = sample.Position;
        if (!float.IsFinite(pos.X) || !float.IsFinite(pos.Y))
        {
            point = default;
            return false;
        }

        pos = Vector2.Clamp(pos, new Vector2(-CoordinateLimit), new Vector2(CoordinateLimit));
        var pressure = float.IsFinite(sample.Pressure) ? Math.Clamp(sample.Pressure, 0f, 1f) : 1f;
        point = new StrokePoint(pos, pressure);
        return true;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
