using System.Numerics;
using Content.Shared.ADT.Body.Pulse;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Surgery.UI;

public sealed class SurgeryHeartMonitor : Control
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const int Columns = 320;
    private const int GapColumns = 12;
    private const float ColumnSeconds = 1f / 80f;
    private const float SteadyBeatSeconds = 0.9f;
    private const float MinIrregularBeatFactor = 0.8f;
    private const float MaxIrregularBeatFactor = 1.65f;
    private const float WeakAmplitude = 0.65f;
    private const float ThreadyAmplitude = 0.6f;
    private const float OldestTraceFade = 0.8f;

    private readonly float[] _trace = new float[Columns];
    private float _pendingColumns;
    private int _head;
    private int _written;
    private float _beatTime;
    private float _beatLength = SteadyBeatSeconds;
    private PulseReading? _staticPulse;

    public PulseReading Pulse { get; set; }

    public SurgeryHeartMonitor()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_cfg.GetCVar(CCVars.ReducedMotion))
        {
            if (_staticPulse == Pulse)
                return;

            _staticPulse = Pulse;
            WriteColumns(Columns);
            return;
        }

        _staticPulse = null;
        _pendingColumns = Math.Min(_pendingColumns + args.DeltaSeconds / ColumnSeconds, Columns);

        var whole = (int) _pendingColumns;
        _pendingColumns -= whole;
        WriteColumns(whole);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var color = SurgeryConsoleStyle.ForPulse(Pulse);
        var lineOffset = new Vector2(0f, 1f);

        for (var age = 1; age < _written && age < Columns - GapColumns; age++)
        {
            var index = (_head - age + Columns) % Columns;
            if (index == 0)
                continue;

            var from = ColumnPosition(index - 1);
            var to = ColumnPosition(index);
            var faded = color.WithAlpha(1f - OldestTraceFade * age / Columns);

            handle.DrawLine(from, to, faded);
            handle.DrawLine(from + lineOffset, to + lineOffset, faded);
        }
    }

    private Vector2 ColumnPosition(int column)
    {
        var x = column * (PixelWidth - 1f) / (Columns - 1);
        var y = PixelHeight * (0.5f - 0.42f * _trace[column]);
        return new Vector2(x, y);
    }

    private void WriteColumns(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _trace[_head] = NextSample();
            _head = (_head + 1) % Columns;
            _written = Math.Min(_written + 1, Columns);
        }
    }

    private float NextSample()
    {
        if (Pulse.Strength == PulseStrength.Absent)
        {
            _beatTime = 0f;
            return 0f;
        }

        _beatTime += ColumnSeconds;
        if (_beatTime >= _beatLength)
        {
            _beatTime -= _beatLength;
            _beatLength = NextBeatLength();
        }

        return Amplitude() * Waveform(_beatTime / Math.Min(_beatLength, SteadyBeatSeconds));
    }

    private float NextBeatLength()
    {
        var beatLength = 60f / Pulse.Bpm;

        return Pulse.Strength == PulseStrength.Irregular
            ? beatLength * _random.NextFloat(MinIrregularBeatFactor, MaxIrregularBeatFactor)
            : beatLength;
    }

    private float Amplitude()
    {
        var amplitude = Pulse.Strength == PulseStrength.Weak ? WeakAmplitude : 1f;
        return Pulse.Pressure == PulsePressure.Thready ? amplitude * ThreadyAmplitude : amplitude;
    }

    private static float Waveform(float phase)
    {
        return 0.14f * Peak(phase, 0.18f, 0.03f)
               - 0.16f * Peak(phase, 0.36f, 0.014f)
               + Peak(phase, 0.40f, 0.014f)
               - 0.26f * Peak(phase, 0.44f, 0.014f)
               + 0.28f * Peak(phase, 0.64f, 0.05f);
    }

    private static float Peak(float phase, float center, float width)
    {
        var distance = (phase - center) / width;
        return MathF.Exp(-0.5f * distance * distance);
    }
}
