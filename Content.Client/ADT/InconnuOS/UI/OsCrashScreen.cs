using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsCrashScreen : Control
{
    private readonly Font _face;
    private readonly Font _title;
    private readonly Font _text;
    private readonly Font _mono;

    private float _time;

    public event Action? OnRebootPressed;

    public OsCrashScreen(IResourceCache cache)
    {
        MouseFilter = MouseFilterMode.Stop;

        _face = OsStyle.CreateFont(cache, 48);
        _title = OsStyle.CreateBoldFont(cache, 18);
        _text = OsStyle.CreateFont(cache, 11);
        _mono = OsStyle.CreateMonoFont(cache, 10);

        var reboot = new Button
        {
            Text = Loc.GetString("os-crash-reboot"),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(0f, 0f, 0f, 40f),
        };

        reboot.OnPressed += _ => OnRebootPressed?.Invoke();

        AddChild(reboot);
    }

    public void Reset()
    {
        _time = 0f;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _time += args.DeltaSeconds;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var width = PixelWidth;

        handle.DrawRect(new UIBox2(0f, 0f, width, PixelHeight), OsStyle.CrashBackground);

        var left = 60f * scale;
        var top = PixelHeight * 0.22f;

        handle.DrawString(_face, new Vector2(left, top), ":(", 1f, Color.White);

        var title = Loc.GetString("os-crash-title", ("os", OsBrand.Name));
        handle.DrawString(_title, new Vector2(left, top + 70f * scale), title, 1f, Color.White);

        var lines = new[]
        {
            Loc.GetString("os-crash-line-1"),
            Loc.GetString("os-crash-line-2", ("publisher", OsBrand.Publisher)),
        };

        for (var i = 0; i < lines.Length; i++)
        {
            handle.DrawString(_text, new Vector2(left, top + (100f + i * 18f) * scale), lines[i], 1f, Color.White);
        }

        var code = Loc.GetString("os-crash-code");
        handle.DrawString(_mono, new Vector2(left, top + 150f * scale), code, 1f, Color.White.WithAlpha(0.75f));

        var uptime = Loc.GetString("os-crash-since", ("seconds", (int) _time));
        handle.DrawString(_mono, new Vector2(left, top + 168f * scale), uptime, 1f, Color.White.WithAlpha(0.5f));

        base.Draw(handle);
    }
}
