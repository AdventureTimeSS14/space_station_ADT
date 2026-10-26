using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client.ADT.Medical.CrewMonitoring;

/// <summary>
/// Simple vertical volume slider (Robust Slider is horizontal-only).
/// </summary>
public sealed class CrewMonitoringVerticalSlider : Control
{
    private bool _grabbed;
    private float _value = 1f;

    public Color TrackColor { get; set; } = Color.FromHex("#1A1A1A");
    public Color FillColor { get; set; } = Color.FromHex("#4CAF50");
    public Color GrabberColor { get; set; } = Color.FromHex("#7CFF8A");

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (MathHelper.CloseToPercent(_value, clamped))
                return;
            _value = clamped;
            OnValueChanged?.Invoke(_value);
        }
    }

    public event Action<float>? OnValueChanged;

    public void SetValueSilent(float value)
    {
        _value = Math.Clamp(value, 0f, 1f);
    }

    public CrewMonitoringVerticalSlider()
    {
        MouseFilter = MouseFilterMode.Stop;
        MinWidth = 18;
        MinHeight = 80;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = PixelSizeBox;
        var trackWidth = Math.Max(6f, box.Width * 0.35f);
        var trackLeft = box.Left + (box.Width - trackWidth) * 0.5f;
        var track = new UIBox2(trackLeft, box.Top + 4f, trackLeft + trackWidth, box.Bottom - 4f);
        handle.DrawRect(track, TrackColor);

        var fillHeight = track.Height * _value;
        var fill = new UIBox2(track.Left, track.Bottom - fillHeight, track.Right, track.Bottom);
        handle.DrawRect(fill, FillColor);

        var grabberH = Math.Max(8f, box.Width * 0.55f);
        var grabberY = track.Bottom - fillHeight - grabberH * 0.5f;
        grabberY = Math.Clamp(grabberY, track.Top, track.Bottom - grabberH);
        var grabber = new UIBox2(
            box.Left + 1f,
            grabberY,
            box.Right - 1f,
            grabberY + grabberH);
        handle.DrawRect(grabber, GrabberColor);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _grabbed = true;
        ApplyPosition(args.RelativePosition.Y);
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function == EngineKeyFunctions.UIClick)
            _grabbed = false;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (!_grabbed)
            return;

        ApplyPosition(args.RelativePosition.Y);
        args.Handle();
    }

    private void ApplyPosition(float localY)
    {
        var pad = 4f;
        var usable = Math.Max(1f, PixelHeight - pad * 2f);
        var ratio = 1f - Math.Clamp((localY - pad) / usable, 0f, 1f);
        Value = ratio;
    }
}
