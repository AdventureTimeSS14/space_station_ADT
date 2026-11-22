using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsPowerScreen : Control
{
    public const float Duration = 2.8f;

    private readonly Font _text;

    private OsPowerAction _action;
    private Color _accent;
    private float _time;
    private bool _sent;

    public event Action<OsPowerAction>? OnFinished;

    public OsPowerScreen(IResourceCache cache)
    {
        MouseFilter = MouseFilterMode.Stop;
        Visible = false;

        _text = OsStyle.CreateFont(cache, 16);
    }

    public void Begin(OsPowerAction action, Color accent)
    {
        _action = action;
        _accent = accent;
        _time = 0f;
        _sent = false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _time += args.DeltaSeconds;

        if (_sent || _time < Duration)
            return;

        _sent = true;
        OnFinished?.Invoke(_action);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var scale = UIScale;
        var fadeIn = Math.Clamp(_time / 0.35f, 0f, 1f);

        var fadeOut = _action == OsPowerAction.Shutdown
            ? 1f - Math.Clamp((_time - (Duration - 0.6f)) / 0.6f, 0f, 1f)
            : 1f;

        var background = OsStyle.Mix(Color.Black, _accent, 0.45f);
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        handle.DrawRect(box, Color.Black.WithAlpha(fadeIn));
        handle.DrawRect(box, background.WithAlpha(fadeIn * fadeOut));

        var text = _action == OsPowerAction.Shutdown
            ? Loc.GetString("os-power-shutdown")
            : Loc.GetString("os-power-reboot");

        var size = handle.GetDimensions(_text, text, 1f);
        var center = new Vector2(PixelWidth / 2f, PixelHeight * 0.5f);
        var alpha = fadeIn * fadeOut;

        OsDraw.Spinner(handle, center - new Vector2(0f, 34f * scale), 16f * scale, 2.2f * scale, _time, Color.White.WithAlpha(alpha));

        handle.DrawString(_text, new Vector2(center.X - size.X / 2f, center.Y), text, 1f, Color.White.WithAlpha(alpha));
    }
}
