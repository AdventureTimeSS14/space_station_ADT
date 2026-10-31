using System.Numerics;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Drawing.Input;

public sealed class PenInputProcessor
{
    private readonly PenInputSystem _system;
    private readonly IGameTiming _timing;

    private readonly SimulatedPressure _simulation = new();
    private readonly StrokeStabilizer _stabilizer = new();

    private const float JumpThreshold = 32f;
    private const float SpikeDistance = 96f;
    private const float SpikeSpeed = 8f;

    private static readonly TimeSpan JumpWindow = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan GapWindow = TimeSpan.FromMilliseconds(200);

    private Vector2 _lastScreen;
    private TimeSpan _lastTime;
    private bool _pendingDown;
    private Vector2 _downScreen;
    private Vector2 _downCanvas;
    private TimeSpan _downTime;
    private PenTool _tool;
    private bool _constant;
    private bool _held;
    private Vector2 _heldScreen;
    private Vector2 _heldCanvas;
    private TimeSpan _heldTime;

    public PenInputProcessor(PenInputSystem system, IGameTiming timing)
    {
        _system = system;
        _timing = timing;
    }

    public event Action<PenSample>? OnSample;

    public bool IsDown { get; private set; }

    public float Stabilization
    {
        get => _stabilizer.StringLength;
        set => _stabilizer.StringLength = float.IsFinite(value) ? MathF.Max(0f, value) : 0f;
    }

    public PenSample Last { get; private set; }

    public float TaperOutLength => _constant ? 0f : _simulation.TaperOutLength;

    public void Down(Vector2 screen, Vector2 canvas, PenTool tool, bool constantPressure)
    {
        if (!IsFinite(screen) || !IsFinite(canvas))
            return;

        _simulation.CopySettingsFrom(_system.SimulationTemplate);
        _simulation.Reset();

        _tool = tool;
        _constant = constantPressure || !_system.SimulatePressure;
        _held = false;
        IsDown = true;

        _pendingDown = true;
        _downScreen = screen;
        _downCanvas = canvas;
        _downTime = _timing.RealTime;
    }

    public void FlushPendingDown()
    {
        if (_pendingDown && _timing.RealTime - _downTime > JumpWindow)
            Start(_downScreen, _downCanvas, _downTime);
    }

    private void Start(Vector2 screen, Vector2 canvas, TimeSpan time)
    {
        _pendingDown = false;
        _stabilizer.Begin(canvas);
        _lastScreen = screen;
        _lastTime = time;
        Emit(canvas, PenPhase.Down, 0f, TimeSpan.Zero, time);
    }

    public void Move(Vector2 screen, Vector2 canvas)
    {
        if (!IsFinite(screen) || !IsFinite(canvas))
            return;

        var now = _timing.RealTime;
        if (IsDown && _pendingDown)
        {
            if (now - _downTime <= JumpWindow && Vector2.Distance(screen, _downScreen) > JumpThreshold)
            {
                Start(screen, canvas, now);
                return;
            }

            Start(_downScreen, _downCanvas, _downTime);
        }

        if (IsDown && _held)
        {
            _held = false;
            if (Vector2.Distance(screen, _heldScreen) <= Vector2.Distance(screen, _lastScreen))
                Process(_heldScreen, _heldCanvas, _heldTime);
        }
        else if (IsDown && IsSpike(screen, now))
        {
            _held = true;
            _heldScreen = screen;
            _heldCanvas = canvas;
            _heldTime = now;
            return;
        }

        Process(screen, canvas, now);
    }

    private bool IsSpike(Vector2 screen, TimeSpan now)
    {
        var distance = Vector2.Distance(screen, _lastScreen);
        var ms = MathF.Max(1f, (float) (now - _lastTime).TotalMilliseconds);
        return distance > SpikeDistance && distance > SpikeSpeed * ms;
    }

    private void Process(Vector2 screen, Vector2 canvas, TimeSpan now)
    {
        var distance = Vector2.Distance(screen, _lastScreen);
        var dt = now - _lastTime;

        if (IsDown && dt > GapWindow && distance > JumpThreshold)
            Cancel();

        _lastScreen = screen;
        _lastTime = now;

        if (!IsDown)
        {
            Emit(canvas, PenPhase.Hover, distance, dt, now);
            return;
        }

        if (!_stabilizer.Move(canvas, out var brush))
            return;

        Emit(brush, PenPhase.Move, distance, dt, now);
    }

    public void Up(Vector2 screen, Vector2 canvas)
    {
        if (!IsDown)
            return;

        _held = false;

        if (!IsFinite(screen) || !IsFinite(canvas))
        {
            Cancel();
            return;
        }

        if (_pendingDown)
        {
            Start(_downScreen, _downCanvas, _downTime);
            screen = _downScreen;
            canvas = _downCanvas;
        }

        var now = _timing.RealTime;
        var distance = Vector2.Distance(screen, _lastScreen);
        var dt = now - _lastTime;
        if (distance > JumpThreshold)
        {
            Cancel();
            return;
        }

        _lastScreen = screen;
        _lastTime = now;

        var end = _stabilizer.End(canvas);
        IsDown = false;

        Emit(end, PenPhase.Up, distance, dt, now);
    }

    public void Cancel()
    {
        if (!IsDown)
            return;

        IsDown = false;
        _held = false;
        if (_pendingDown)
        {
            _pendingDown = false;
            return;
        }

        _stabilizer.End(Last.Position);
        Emit(Last.Position, PenPhase.Up, 0f, TimeSpan.Zero, _timing.RealTime);
    }

    private void Emit(Vector2 canvas, PenPhase phase, float screenDistance, TimeSpan dt, TimeSpan now)
    {
        float raw;
        PressureOrigin origin;

        if (phase == PenPhase.Hover)
        {
            origin = _constant ? PressureOrigin.Constant : PressureOrigin.Simulated;
            raw = 0f;
        }
        else if (_constant)
        {
            origin = PressureOrigin.Constant;
            raw = 1f;
        }
        else
        {
            origin = PressureOrigin.Simulated;
            raw = _simulation.Next(screenDistance, dt);
        }

        var pressure = origin == PressureOrigin.Constant ? raw : _system.Curve.Evaluate(raw);

        Last = new PenSample(canvas, pressure, raw, _tool, phase, origin, now);
        OnSample?.Invoke(Last);
    }

    private static bool IsFinite(Vector2 v)
    {
        return float.IsFinite(v.X) && float.IsFinite(v.Y);
    }
}
