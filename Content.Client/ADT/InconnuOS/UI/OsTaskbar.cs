using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsTaskbar : Control
{
    private readonly OsContext _context;
    private readonly OsWindowManager _manager;
    private readonly IGameTiming _timing;

    private readonly BoxContainer _tasks;
    private readonly OsIconControl _floppy;
    private readonly Label _clock;

    public event Action? OnStartPressed;

    public OsTaskbar(OsContext context, OsWindowManager manager, IGameTiming timing)
    {
        _context = context;
        _manager = manager;
        _timing = timing;

        MouseFilter = MouseFilterMode.Stop;
        MinHeight = OsStyle.TaskbarHeight;

        var start = new OsStartButton(context);
        start.OnPressed += () => OnStartPressed?.Invoke();

        _tasks = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(6f, 3f, 6f, 3f),
        };

        _floppy = new OsIconControl(OsAppIcon.Disk, 16f)
        {
            Accent = context.Accent,
            Margin = new Thickness(0f, 0f, 8f, 0f),
            Visible = false,
        };

        _clock = new Label
        {
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(0f, 0f, 10f, 0f),
        };

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalExpand = true,
            Children =
            {
                start,
                _tasks,
                _floppy,
                _clock,
            },
        });

        _manager.OnWindowsChanged += Rebuild;

        Rebuild();
    }

    private void Rebuild()
    {
        var windows = new List<OsWindow>();

        foreach (var window in _manager.Windows)
        {
            if (!window.Closing)
                windows.Add(window);
        }

        if (SameWindows(windows))
            return;

        _tasks.RemoveAllChildren();

        foreach (var window in windows)
        {
            var button = new OsTaskButton(window, _context);
            var target = window;

            button.OnPressed += () => _manager.Toggle(target);

            _tasks.AddChild(button);
        }
    }

    private bool SameWindows(List<OsWindow> windows)
    {
        if (_tasks.ChildCount != windows.Count)
            return false;

        for (var i = 0; i < windows.Count; i++)
        {
            if (_tasks.GetChild(i) is not OsTaskButton button || button.Window != windows[i])
                return false;
        }

        return true;
    }

    public UIBox2? GetButtonRect(OsWindow window)
    {
        foreach (var child in _tasks.Children)
        {
            if (child is not OsTaskButton button || button.Window != window)
                continue;

            if (button.PixelWidth <= 0 || button.PixelHeight <= 0)
                return null;

            return UIBox2.FromDimensions(button.GlobalPixelPosition, button.PixelSize);
        }

        return null;
    }

    public void StateChanged()
    {
        _floppy.Accent = _context.Accent;
        _floppy.Visible = HasRemovable();

        Rebuild();
    }

    private bool HasRemovable()
    {
        foreach (var drive in _context.State.Drives)
        {
            if (drive.Removable)
                return true;
        }

        return false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_context.State.Settings.ShowClock)
        {
            _clock.Visible = false;
            return;
        }

        _clock.Visible = true;

        var time = _timing.CurTime;

        _clock.Text = $"{time.Hours + time.Days * 24:00}:{time.Minutes:00}";
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;

        handle.DrawRect(new UIBox2(0f, 0f, PixelWidth, PixelHeight), OsStyle.Taskbar);
        handle.DrawRect(new UIBox2(0f, 0f, PixelWidth, scale), OsStyle.TaskbarLine);

        base.Draw(handle);
    }
}

public sealed class OsStartButton : Control
{
    private readonly OsContext _context;
    private readonly Label _label;

    private bool _hovered;

    public event Action? OnPressed;

    public OsStartButton(OsContext context)
    {
        _context = context;

        MouseFilter = MouseFilterMode.Stop;
        MinWidth = 86f;

        _label = new Label
        {
            Text = Loc.GetString("os-taskbar-start"),
            Modulate = OsStyle.TextBright,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(22f, 0f, 6f, 0f),
        };

        AddChild(_label);
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
        var scale = UIScale;
        var accent = _context.Accent;

        var box = new UIBox2(2f * scale, 3f * scale, PixelWidth - 2f * scale, PixelHeight - 3f * scale);

        OsDraw.RoundedRect(handle, box, 3f * scale,
            _hovered ? OsStyle.Mix(OsStyle.TaskbarButton, accent, 0.45f) : OsStyle.TaskbarButton);

        var size = 5f * scale;
        var left = 8f * scale;
        var top = PixelHeight / 2f - size - scale;

        handle.DrawRect(new UIBox2(left, top, left + size, top + size), accent);
        handle.DrawRect(new UIBox2(left + size + scale, top, left + size * 2f + scale, top + size), OsStyle.Mix(accent, Color.White, 0.4f));
        handle.DrawRect(new UIBox2(left, top + size + scale, left + size, top + size * 2f + scale), OsStyle.Mix(accent, Color.White, 0.4f));
        handle.DrawRect(new UIBox2(left + size + scale, top + size + scale, left + size * 2f + scale, top + size * 2f + scale), accent);

        base.Draw(handle);
    }
}

public sealed class OsTaskButton : Control
{
    private readonly OsWindow _window;
    private readonly OsContext _context;
    private readonly OsIconControl _icon;
    private readonly Label _label;

    private bool _hovered;

    public OsWindow Window => _window;

    public event Action? OnPressed;

    public OsTaskButton(OsWindow window, OsContext context)
    {
        _window = window;
        _context = context;

        MouseFilter = MouseFilterMode.Stop;
        MinWidth = 140f;
        MaxWidth = 200f;
        Margin = new Thickness(0f, 0f, 4f, 0f);

        _icon = new OsIconControl(window.Proto.Icon, 14f)
        {
            Accent = context.Accent,
            Margin = new Thickness(8f, 0f, 0f, 0f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Center,
        };

        _label = new Label
        {
            Text = window.Title,
            Modulate = OsStyle.Text,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(30f, 0f, 8f, 0f),
        };

        AddChild(_icon);
        AddChild(_label);
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

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_label.Text != _window.Title)
            _label.Text = _window.Title;

        _label.Modulate = _window.Minimized ? OsStyle.TextDim : OsStyle.Text;
        _icon.Accent = _context.Accent;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var box = new UIBox2(0f, 3f * scale, PixelWidth, PixelHeight - 3f * scale);
        var accent = _context.Accent;

        var color = OsStyle.TaskbarButton;

        if (_window.Active)
            color = OsStyle.Mix(OsStyle.TaskbarButton, accent, 0.35f);
        else if (_hovered)
            color = OsStyle.TaskbarButtonHover;

        OsDraw.RoundedRect(handle, box, 4f * scale, color);

        var line = _window.Active ? PixelWidth * 0.5f : PixelWidth * 0.18f;
        var lineColor = _window.Minimized ? OsStyle.TextDim : accent;

        handle.DrawRect(new UIBox2(
            PixelWidth / 2f - line / 2f,
            box.Bottom - 2f * scale,
            PixelWidth / 2f + line / 2f,
            box.Bottom), lineColor);

        base.Draw(handle);
    }
}
