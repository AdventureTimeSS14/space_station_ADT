using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsWindowLayer : Control
{
    [Dependency] private readonly IClyde _clyde = default!;

    public event Action<OsWindow>? OnWindowRemoved;

    private readonly List<OsWindow> _removed = new();

    public OsWindowLayer()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Ignore;
        RectClipContent = true;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        foreach (var child in Children)
        {
            if (child is not OsWindow window)
            {
                child.Arrange(new UIBox2(Vector2.Zero, finalSize));
                continue;
            }

            child.Arrange(GetRect(window, finalSize));
        }

        return finalSize;
    }

    public static UIBox2 GetRect(OsWindow window, Vector2 finalSize)
    {
        if (window.Maximized)
            return new UIBox2(Vector2.Zero, finalSize);

        window.ClampSize();

        var size = Vector2.Min(window.WindowSize, finalSize);
        var max = Vector2.Max(finalSize - size, Vector2.Zero);
        var position = Vector2.Clamp(window.WindowPosition, Vector2.Zero, max);

        window.WindowPosition = position;

        return new UIBox2(position, position + size);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _removed.Clear();

        foreach (var child in Children)
        {
            if (child is OsWindow { PendingRemoval: true } window)
                _removed.Add(window);
        }

        foreach (var window in _removed)
        {
            RemoveChild(window);
            OnWindowRemoved?.Invoke(window);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;

        foreach (var child in Children)
        {
            if (child is not OsWindow { Visible: true, Animation: OsWindowAnimation.None } window)
                continue;

            var position = (Vector2) window.PixelPosition;
            var rect = new UIBox2(position, position + window.PixelSize);
            var radius = window.Maximized ? 0f : OsWindow.Radius * scale;

            OsDraw.Shadow(handle, rect, radius, (window.Active ? 16f : 10f) * scale, window.Active ? 0.5f : 0.35f);
        }

        base.Draw(handle);
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        if (GetChild(childIndex) is not OsWindow { Visible: true } window)
        {
            base.RenderChildOverride(ref args, childIndex, position);
            return;
        }

        var size = window.PixelSize;
        var layout = UIBox2.FromDimensions(position, size);

        if (!window.TryGetVisual(layout, out var visual, out var alpha))
        {
            base.RenderChildOverride(ref args, childIndex, position);
            return;
        }

        if (size.X <= 0 || size.Y <= 0)
            return;

        var target = window.GetSnapshot(_clyde, size);
        var renderHandle = args.Handle;
        var ui = UserInterfaceManager;

        renderHandle.RenderInRenderTarget(target, () =>
        {
            renderHandle.SetScissor(null);
            ui.RenderControl(renderHandle, window, Vector2i.Zero);
        }, Color.Transparent);

        var handle = renderHandle.DrawingHandleScreen;
        var opacity = alpha * args.Modulate.A;
        var radius = OsWindow.Radius * UIScale * visual.Width / MathF.Max(1f, layout.Width);

        OsDraw.Shadow(handle, visual, radius, 16f * UIScale, 0.5f * opacity);
        handle.DrawTextureRect(target.Texture, visual, args.Modulate.WithAlpha(opacity));
    }
}

public sealed class OsWindowManager
{
    public readonly OsWindowLayer Layer = new();

    private readonly OsAppRegistry _registry;
    private readonly OsContext _context;
    private readonly List<OsWindow> _windows = new();

    private OsWindow? _drag;
    private OsDragMode _dragMode;
    private Vector2 _dragScreen;
    private Vector2 _dragValue;

    private uint _focusFrame;

    public event Action? OnWindowsChanged;

    public Func<OsWindow, UIBox2?> TaskbarAnchor = _ => null;

    public IReadOnlyList<OsWindow> Windows => _windows;

    public OsWindow? Focused { get; private set; }

    public bool FocusedThisFrame => _focusFrame == _context.Timing.CurFrame;

    public OsWindowManager(OsAppRegistry registry, OsContext context)
    {
        _registry = registry;
        _context = context;

        Layer.OnWindowRemoved += OnWindowRemoved;
    }

    public OsWindow? Find(string appId)
    {
        foreach (var window in _windows)
        {
            if (window.Proto.ID == appId && !window.Closing)
                return window;
        }

        return null;
    }

    public OsWindow Open(ADTOsAppPrototype proto, string? argument, bool animate = true)
    {
        if (proto.SingleInstance && Find(proto.ID) is { } existing)
        {
            if (argument != null)
                existing.App.OnOpen(argument);

            if (existing.Minimized)
                Restore(existing);
            else
                Focus(existing);

            return existing;
        }

        var app = _registry.Create(proto);

        app.Context = _context;
        app.Proto = proto;

        var window = new OsWindow(proto, app, _context.Accent)
        {
            Animate = animate && _context.State.Settings.Animations,
        };

        window.WindowPosition = NextPosition();
        window.OnCloseRequested += Close;
        window.OnMinimizeRequested += Minimize;
        window.OnFocusRequested += Focus;
        window.OnDragRequested += BeginDrag;
        window.OnChanged += _ => OnWindowsChanged?.Invoke();
        window.OnPointerMoved += PointerMove;
        window.OnPointerUp += PointerUp;

        _windows.Add(window);
        Layer.AddChild(window);

        app.OnOpen(argument);

        window.BeginOpen();
        window.Animate = _context.State.Settings.Animations;

        Focus(window);

        return window;
    }

    private Vector2 NextPosition()
    {
        var step = _windows.Count % 8;

        return new Vector2(40f + step * 26f, 30f + step * 22f);
    }

    public void Close(OsWindow window)
    {
        if (window.Closing || !window.App.OnClosing())
            return;

        window.BeginClose();

        if (Focused == window)
            Focused = null;

        FocusTopmost();

        OnWindowsChanged?.Invoke();
    }

    public void Minimize(OsWindow window)
    {
        if (window.Minimized || window.Closing)
            return;

        window.BeginMinimize(TaskbarAnchor(window) ?? default);

        if (Focused == window)
            Focused = null;

        FocusTopmost();

        OnWindowsChanged?.Invoke();
    }

    public void MinimizeInstant(OsWindow window)
    {
        window.SetMinimizedInstant(true);

        if (Focused == window)
            Focused = null;

        FocusTopmost();

        OnWindowsChanged?.Invoke();
    }

    public void Restore(OsWindow window)
    {
        if (!window.Minimized)
            return;

        window.BeginRestore(TaskbarAnchor(window) ?? default);

        Focus(window);
    }

    public void Toggle(OsWindow window)
    {
        if (window.Minimized)
        {
            Restore(window);
            return;
        }

        if (Focused == window)
        {
            Minimize(window);
            return;
        }

        Focus(window);
    }

    public void Focus(OsWindow window)
    {
        if (window.Minimized || window.Closing)
            return;

        var changed = Focused != window;

        Focused = window;
        _focusFrame = _context.Timing.CurFrame;

        foreach (var other in _windows)
        {
            other.Active = other == window;
        }

        if (window.GetPositionInParent() != Layer.ChildCount - 1)
            window.SetPositionLast();

        if (changed)
            OnWindowsChanged?.Invoke();
    }

    private void FocusTopmost()
    {
        for (var i = Layer.ChildCount - 1; i >= 0; i--)
        {
            if (Layer.GetChild(i) is not OsWindow { Minimized: false, Closing: false } window)
                continue;

            Focus(window);
            return;
        }

        foreach (var window in _windows)
        {
            window.Active = false;
        }
    }

    private void OnWindowRemoved(OsWindow window)
    {
        _windows.Remove(window);
        window.Dispose();

        OnWindowsChanged?.Invoke();
    }

    private void BeginDrag(OsWindow window, OsDragMode mode, Vector2 screen)
    {
        if (window.Maximized && mode == OsDragMode.Move)
            return;

        _drag = window;
        _dragMode = mode;
        _dragScreen = screen;
        _dragValue = mode == OsDragMode.Move ? window.WindowPosition : window.WindowSize;
    }

    public void PointerMove(Vector2 screen)
    {
        if (_drag == null)
            return;

        var delta = (screen - _dragScreen) / _drag.UIScale;

        if (_dragMode == OsDragMode.Move)
        {
            _drag.WindowPosition = _dragValue + delta;
        }
        else
        {
            _drag.WindowSize = _dragValue + delta;
            _drag.ClampSize();
        }

        Layer.InvalidateArrange();
    }

    public void PointerUp()
    {
        _drag = null;
        _dragMode = OsDragMode.None;
    }

    public void StateChanged()
    {
        foreach (var window in _windows)
        {
            window.Animate = _context.State.Settings.Animations;
            window.SetAccent(_context.Accent);
            window.App.OnStateChanged();
        }
    }

    public void CloseAll()
    {
        foreach (var window in _windows.ToArray())
        {
            _windows.Remove(window);
            Layer.RemoveChild(window);
            window.Dispose();
        }

        Focused = null;
        _drag = null;

        OnWindowsChanged?.Invoke();
    }
}
