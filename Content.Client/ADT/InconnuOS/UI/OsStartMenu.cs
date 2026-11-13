using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsStartMenu : Control
{
    public const float MenuWidth = 232f;

    private readonly OsContext _context;

    private readonly BoxContainer _apps;
    private readonly Label _user;
    private readonly Label _machine;

    public float Appear;

    public bool Opened { get; private set; }
    public bool Animate = true;

    public event Action<ADTOsAppPrototype>? OnAppPicked;
    public event Action<OsPowerAction>? OnPowerPicked;

    public OsStartMenu(OsContext context)
    {
        _context = context;

        MouseFilter = MouseFilterMode.Stop;
        Visible = false;
        RectClipContent = true;

        _user = new Label
        {
            Modulate = OsStyle.TextBright,
            Margin = new Thickness(10f, 8f, 10f, 0f),
        };

        _machine = new Label
        {
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(10f, 0f, 10f, 8f),
        };

        _apps = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Margin = new Thickness(4f, 4f, 4f, 4f),
        };

        var reboot = new OsMenuItem(OsAppIcon.Tasks, Loc.GetString("os-start-reboot"), context.Accent);
        var shutdown = new OsMenuItem(OsAppIcon.Computer, Loc.GetString("os-start-shutdown"), context.Accent);

        reboot.OnPressed += () => OnPowerPicked?.Invoke(OsPowerAction.Reboot);
        shutdown.OnPressed += () => OnPowerPicked?.Invoke(OsPowerAction.Shutdown);

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children =
            {
                _user,
                _machine,
                _apps,
                reboot,
                shutdown,
            },
        });
    }

    public void Toggle()
    {
        if (Opened)
        {
            Close();
            return;
        }

        Opened = true;
        Visible = true;

        if (!Animate)
            Appear = 1f;

        Rebuild();
    }

    public void Close()
    {
        Opened = false;

        if (Animate)
            return;

        Appear = 0f;
        Visible = false;
    }

    public void Rebuild()
    {
        _user.Text = _context.State.UserName;
        _machine.Text = _context.State.MachineName;

        _apps.RemoveAllChildren();

        var apps = new List<ADTOsAppPrototype>();

        foreach (var id in _context.State.Apps)
        {
            if (!_context.Prototypes.TryIndex(id, out var app) || app.Hidden)
                continue;

            apps.Add(app);
        }

        apps.Sort(Compare);

        foreach (var app in apps)
        {
            var item = new OsMenuItem(app.Icon, Loc.GetString(app.Name), _context.Accent);
            var picked = app;

            item.OnPressed += () => OnAppPicked?.Invoke(picked);

            _apps.AddChild(item);
        }
    }

    private static int Compare(ADTOsAppPrototype a, ADTOsAppPrototype b)
    {
        if (a.Priority != b.Priority)
            return b.Priority.CompareTo(a.Priority);

        return string.Compare(Loc.GetString(a.Name), Loc.GetString(b.Name), StringComparison.CurrentCultureIgnoreCase);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var target = Opened ? 1f : 0f;

        if (MathF.Abs(Appear - target) <= 0.004f)
        {
            if (Appear == target)
                return;

            Appear = target;
        }
        else
        {
            Appear = MathHelper.Lerp(Appear, target, 1f - MathF.Exp(-args.DeltaSeconds * 20f));
        }

        Modulate = Color.White.WithAlpha(Math.Clamp(Appear, 0f, 1f));

        if (Appear <= 0.01f && !Opened)
            Visible = false;

        Parent?.InvalidateArrange();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        OsDraw.RoundedRect(handle, box, 4f * scale, OsStyle.WindowBorder);
        OsDraw.RoundedRect(handle, new UIBox2(scale, scale, PixelWidth - scale, PixelHeight - scale), 4f * scale, OsStyle.Panel);

        handle.DrawRect(new UIBox2(scale, scale, 4f * scale, PixelHeight - scale), _context.Accent);

        base.Draw(handle);
    }
}

public sealed class OsMenuItem : Control
{
    private readonly Color _accent;
    private bool _hovered;

    public event Action? OnPressed;

    public OsMenuItem(OsAppIcon icon, string text, Color accent)
    {
        _accent = accent;

        MouseFilter = MouseFilterMode.Stop;
        MinHeight = 28f;

        AddChild(new OsIconControl(icon, 18f)
        {
            Accent = accent,
            Margin = new Thickness(10f, 0f, 0f, 0f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Center,
        });

        AddChild(new Label
        {
            Text = text,
            Modulate = OsStyle.Text,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(36f, 0f, 8f, 0f),
        });
    }

    protected override void MouseEntered()
    {
        base.MouseEntered();
        _hovered = true;
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hovered = false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnPressed?.Invoke();
        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_hovered)
        {
            var scale = UIScale;

            OsDraw.RoundedRect(handle, new UIBox2(2f * scale, scale, PixelWidth - 2f * scale, PixelHeight - scale),
                3f * scale, OsStyle.Mix(OsStyle.PanelLight, _accent, 0.35f));
        }

        base.Draw(handle);
    }
}
