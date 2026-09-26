using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsDesktop : Control
{
    private readonly OsContext _context;
    private readonly OsWindowManager _manager;

    private readonly Control _icons;
    private readonly OsWindowLayer _layer;
    private readonly OsStartMenu _start;
    private readonly OsTaskbar _taskbar;
    private readonly OsToastLayer _toasts;
    private readonly OsPopupLayer _popup;

    private readonly Font _font;
    private readonly Font _fontBold;

    private string? _selectedIcon;

    public event Action<OsPowerAction>? OnPowerPicked;

    public OsDesktop(OsContext context, OsWindowManager manager, IGameTiming timing)
    {
        _context = context;
        _manager = manager;

        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;

        _font = OsStyle.CreateFont(context.Cache, 10);
        _fontBold = OsStyle.CreateBoldFont(context.Cache, 12);

        _icons = new Control
        {
            MouseFilter = MouseFilterMode.Ignore,
        };

        _layer = manager.Layer;

        _start = new OsStartMenu(context);
        _taskbar = new OsTaskbar(context, manager, timing);
        _toasts = new OsToastLayer();
        _popup = new OsPopupLayer();

        _taskbar.OnStartPressed += () => _start.Toggle();

        _manager.TaskbarAnchor = window => _taskbar.GetButtonRect(window);

        _start.OnAppPicked += app =>
        {
            _start.Close();
            _manager.Open(app, null);
        };

        _start.OnPowerPicked += action =>
        {
            _start.Close();
            OnPowerPicked?.Invoke(action);
        };

        AddChild(_icons);
        AddChild(_layer);
        AddChild(_start);
        AddChild(_taskbar);
        AddChild(_toasts);
        AddChild(_popup);

        RebuildIcons();
    }

    public void Toast(string text)
    {
        _toasts.Add(text, _context.Accent, _context.State.Settings.Animations);
    }

    public void CloseMenus()
    {
        _start.Close();
        _popup.Close();
    }

    public void ShowMenu(Vector2 screen, List<OsMenuEntry> entries)
    {
        _start.Close();
        _popup.Show(screen, _context.Accent, entries);
    }

    public void StateChanged()
    {
        _taskbar.StateChanged();
        _start.Animate = _context.State.Settings.Animations;

        if (_start.Opened)
            _start.Rebuild();

        RebuildIcons();
    }

    private void RebuildIcons()
    {
        _icons.RemoveAllChildren();

        foreach (var id in _context.State.Apps)
        {
            if (!_context.Prototypes.TryIndex(id, out var app) || !app.OnDesktop)
                continue;

            var icon = new OsDesktopIcon(app, _context.Accent)
            {
                Selected = _selectedIcon == app.ID,
            };

            var picked = app;

            icon.OnSelected += () => SelectIcon(picked.ID);
            icon.OnPressed += () => _manager.Open(picked, null);
            icon.OnContextMenu += screen => ShowIconMenu(picked, screen);

            _icons.AddChild(icon);
        }
    }

    private void SelectIcon(string? id)
    {
        _selectedIcon = id;

        foreach (var child in _icons.Children)
        {
            if (child is OsDesktopIcon icon)
                icon.Selected = icon.Proto.ID == id;
        }
    }

    private void ShowIconMenu(ADTOsAppPrototype app, Vector2 screen)
    {
        var entries = new List<OsMenuEntry>
        {
            new(Loc.GetString("os-desktop-icon-menu-open"), () => _manager.Open(app, null)),
            OsMenuEntry.Line(),
            new(Loc.GetString("os-desktop-icon-menu-properties"), () => ShowIconProperties(app)),
        };

        ShowMenu(screen, entries);
    }

    private void ShowIconProperties(ADTOsAppPrototype app)
    {
        var text = Loc.GetString(app.Name);

        if (app.Description is { } description)
            text += ": " + Loc.GetString(description);

        Toast(text);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var deskHeight = MathF.Max(0f, finalSize.Y - OsStyle.TaskbarHeight);
        var desk = new UIBox2(0f, 0f, finalSize.X, deskHeight);

        _icons.Arrange(desk);
        _layer.Arrange(desk);
        _toasts.Arrange(desk);

        _taskbar.Arrange(new UIBox2(0f, deskHeight, finalSize.X, finalSize.Y));
        _popup.Arrange(new UIBox2(Vector2.Zero, finalSize));

        ArrangeIcons(deskHeight);
        ArrangeStartMenu(deskHeight);

        return finalSize;
    }

    private void ArrangeIcons(float deskHeight)
    {
        var x = OsStyle.DesktopPadding;
        var y = OsStyle.DesktopPadding;

        foreach (var child in _icons.Children)
        {
            if (y + OsStyle.DesktopIconHeight > deskHeight - OsStyle.DesktopPadding)
            {
                y = OsStyle.DesktopPadding;
                x += OsStyle.DesktopIconWidth;
            }

            child.Arrange(new UIBox2(x, y, x + OsStyle.DesktopIconWidth, y + OsStyle.DesktopIconHeight));

            y += OsStyle.DesktopIconHeight;
        }
    }

    private void ArrangeStartMenu(float deskHeight)
    {
        var height = MathF.Min(340f, MathF.Max(160f, deskHeight - 16f));
        var slide = (1f - Math.Clamp(_start.Appear, 0f, 1f)) * height;

        var top = deskHeight - height + slide;

        _start.Arrange(new UIBox2(4f, top, 4f + OsStartMenu.MenuWidth, top + height));
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            SelectIcon(null);
            ShowMenu(args.PointerLocation.Position, BuildDesktopMenu());
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        SelectIcon(null);
        _start.Close();
    }

    private List<OsMenuEntry> BuildDesktopMenu()
    {
        return new List<OsMenuEntry>
        {
            new(Loc.GetString("os-desktop-menu-explorer"), () => _context.OpenApp("OsAppExplorer", null)),
            new(Loc.GetString("os-desktop-menu-computer"), () => _context.OpenApp("OsAppMyComputer", null)),
            OsMenuEntry.Line(),
            new(Loc.GetString("os-desktop-menu-personalize"), () => _context.OpenApp("OsAppControlPanel", null)),
        };
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _manager.PointerMove(args.GlobalPixelPosition.Position);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
            _manager.PointerUp();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        DrawWallpaper(handle);

        base.Draw(handle);

        DrawWatermark(handle);
    }

    private void DrawWallpaper(DrawingHandleScreen handle)
    {
        var width = PixelWidth;
        var height = PixelHeight;
        var accent = _context.Accent;
        var wallpaper = _context.State.Settings.Wallpaper;

        if (wallpaper == OsWallpaper.Plain)
        {
            handle.DrawRect(new UIBox2(0f, 0f, width, height), OsStyle.Mix(OsStyle.DesktopBottom, accent, 0.25f));
            return;
        }

        const int bands = 24;

        for (var i = 0; i < bands; i++)
        {
            var from = height * i / bands;
            var to = height * (i + 1f) / bands;
            var color = OsStyle.Mix(OsStyle.DesktopTop, OsStyle.DesktopBottom, i / (bands - 1f));

            handle.DrawRect(new UIBox2(0f, from, width, to), color);
        }

        switch (wallpaper)
        {
            case OsWallpaper.Grid:
                DrawGrid(handle, width, height, accent);
                break;

            case OsWallpaper.Aurora:
                DrawAurora(handle, width, height, accent);
                break;

            case OsWallpaper.Circuitry:
                DrawCircuitry(handle, width, height, accent);
                break;

            default:
                DrawGlow(handle, width, height, accent);
                break;
        }
    }

    private static void DrawGrid(DrawingHandleScreen handle, float width, float height, Color accent)
    {
        var step = 48f;
        var color = accent.WithAlpha(0.12f);

        for (var x = step; x < width; x += step)
        {
            handle.DrawLine(new Vector2(x, 0f), new Vector2(x, height), color);
        }

        for (var y = step; y < height; y += step)
        {
            handle.DrawLine(new Vector2(0f, y), new Vector2(width, y), color);
        }
    }

    private static void DrawAurora(DrawingHandleScreen handle, float width, float height, Color accent)
    {
        for (var i = 0; i < 5; i++)
        {
            var offset = height * (0.2f + i * 0.13f);
            var color = accent.WithAlpha(0.10f - i * 0.015f);

            handle.DrawLine(new Vector2(0f, offset), new Vector2(width, offset - height * 0.18f), color);
            handle.DrawLine(new Vector2(0f, offset + 6f), new Vector2(width, offset - height * 0.18f + 6f), color);
        }
    }

    private static void DrawCircuitry(DrawingHandleScreen handle, float width, float height, Color accent)
    {
        var color = accent.WithAlpha(0.14f);
        var step = 64f;

        for (var x = step; x < width; x += step)
        {
            var mid = height * 0.5f + MathF.Sin(x / 90f) * height * 0.2f;

            handle.DrawLine(new Vector2(x, 0f), new Vector2(x, mid), color);
            handle.DrawLine(new Vector2(x, mid), new Vector2(x + step * 0.6f, mid), color);
            handle.DrawCircle(new Vector2(x + step * 0.6f, mid), 3f, color);
        }
    }

    private static void DrawGlow(DrawingHandleScreen handle, float width, float height, Color accent)
    {
        var center = new Vector2(width * 0.72f, height * 0.28f);

        for (var i = 6; i > 0; i--)
        {
            handle.DrawCircle(center, i * height * 0.06f, accent.WithAlpha(0.02f));
        }
    }

    private void DrawWatermark(DrawingHandleScreen handle)
    {
        if (_context.State.Activated)
            return;

        var first = Loc.GetString("os-watermark-title", ("os", OsBrand.Name));
        var second = Loc.GetString("os-watermark-hint", ("publisher", OsBrand.Publisher));

        var firstSize = handle.GetDimensions(_fontBold, first, 1f);
        var secondSize = handle.GetDimensions(_font, second, 1f);

        var bottom = PixelHeight - OsStyle.TaskbarHeight * UIScale - 14f * UIScale;

        handle.DrawString(_fontBold,
            new Vector2(PixelWidth - firstSize.X - 18f * UIScale, bottom - firstSize.Y - secondSize.Y),
            first, 1f, OsStyle.Watermark);

        handle.DrawString(_font,
            new Vector2(PixelWidth - secondSize.X - 18f * UIScale, bottom - secondSize.Y),
            second, 1f, OsStyle.Watermark);
    }
}

public sealed class OsDesktopIcon : Control
{
    public readonly ADTOsAppPrototype Proto;

    private readonly Color _accent;
    private bool _hovered;

    public bool Selected;

    public event Action? OnSelected;
    public event Action? OnPressed;
    public event Action<Vector2>? OnContextMenu;

    public OsDesktopIcon(ADTOsAppPrototype proto, Color accent)
    {
        Proto = proto;
        _accent = accent;

        MouseFilter = MouseFilterMode.Stop;

        AddChild(new OsIconControl(proto.Icon, OsStyle.DesktopIconSize)
        {
            Accent = accent,
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0f, 8f, 0f, 0f),
            VerticalAlignment = VAlignment.Top,
        });

        AddChild(new Label
        {
            Text = Loc.GetString(proto.Name),
            Modulate = OsStyle.Text,
            ClipText = true,
            Align = Label.AlignMode.Center,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(2f, 0f, 2f, 4f),
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

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            OnSelected?.Invoke();
            OnContextMenu?.Invoke(args.PointerLocation.Position);
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (OsDoubleClick.Check("desktop:" + Proto.ID))
        {
            OnPressed?.Invoke();
        }
        else
        {
            OnSelected?.Invoke();
        }

        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_hovered || Selected)
        {
            var scale = UIScale;

            OsDraw.RoundedRect(handle, new UIBox2(2f * scale, 2f * scale, PixelWidth - 2f * scale, PixelHeight - 2f * scale),
                4f * scale, _accent.WithAlpha(Selected ? 0.35f : 0.18f));
        }

        base.Draw(handle);
    }
}

public sealed class OsToastLayer : Control
{
    private const float ToastWidth = 280f;
    private const float ToastHeight = 36f;

    private readonly List<OsToast> _finished = new();

    public OsToastLayer()
    {
        MouseFilter = MouseFilterMode.Ignore;
    }

    public void Add(string text, Color accent, bool animate)
    {
        AddChild(new OsToast(text, accent, animate));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _finished.Clear();

        foreach (var child in Children)
        {
            if (child is OsToast { Finished: true } toast)
                _finished.Add(toast);
        }

        foreach (var toast in _finished)
        {
            RemoveChild(toast);
            toast.Dispose();
        }
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var bottom = finalSize.Y - 10f;

        for (var i = ChildCount - 1; i >= 0; i--)
        {
            if (GetChild(i) is not OsToast toast)
                continue;

            var height = MathF.Max(ToastHeight, toast.DesiredSize.Y);
            var slide = (1f - toast.Appear) * 24f;

            var top = bottom - height;

            toast.Arrange(new UIBox2(
                finalSize.X - ToastWidth - 10f + slide,
                top,
                finalSize.X - 10f + slide,
                bottom));

            bottom -= height + 6f;
        }

        return finalSize;
    }
}

public sealed class OsToast : Control
{
    private readonly Color _accent;
    private readonly bool _animate;

    public float Appear;

    private float _life = 4f;

    public bool Finished { get; private set; }

    public OsToast(string text, Color accent, bool animate)
    {
        _accent = accent;
        _animate = animate;

        MouseFilter = MouseFilterMode.Ignore;

        AddChild(new Label
        {
            Text = text,
            Modulate = OsStyle.Text,
            ClipText = true,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(12f, 0f, 8f, 0f),
        });

        if (!animate)
            Appear = 1f;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _life -= args.DeltaSeconds;

        var target = _life > 0f ? 1f : 0f;

        Appear = _animate
            ? MathHelper.Lerp(Appear, target, 1f - MathF.Exp(-args.DeltaSeconds * 14f))
            : target;
        Modulate = Color.White.WithAlpha(Math.Clamp(Appear, 0f, 1f));

        Parent?.InvalidateArrange();

        if (_life > 0f || Appear > 0.02f)
            return;

        Finished = true;
        Visible = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        OsDraw.Shadow(handle, box, 5f * scale, 8f * scale, 0.4f * Modulate.A);
        OsDraw.Framed(handle, box, 5f * scale, scale, OsStyle.WindowBorder, OsStyle.Panel);

        handle.DrawRect(new UIBox2(scale, 6f * scale, 4f * scale, PixelHeight - 6f * scale), _accent);

        base.Draw(handle);
    }
}
