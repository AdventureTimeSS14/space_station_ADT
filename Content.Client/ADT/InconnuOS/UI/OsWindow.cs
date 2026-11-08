using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public enum OsDragMode : byte
{
    None = 0,
    Move,
    Resize,
}

public enum OsWindowAnimation : byte
{
    None = 0,
    Open,
    Close,
    Minimize,
    Restore,
    Resize,
}

public sealed class OsWindow : Control
{
    public const float Radius = 6f;

    public readonly ADTOsAppPrototype Proto;
    public readonly OsAppControl App;

    public Vector2 WindowPosition;
    public Vector2 WindowSize;

    public bool Minimized;
    public bool Maximized;
    public bool Active;
    public bool Animate = true;

    public string Title { get; private set; }

    public bool Closing { get; private set; }

    public bool PendingRemoval { get; private set; }

    public OsWindowAnimation Animation { get; private set; }

    public float AnimationProgress { get; private set; }

    public UIBox2 AnimationAnchor { get; private set; }

    public event Action<OsWindow>? OnCloseRequested;
    public event Action<OsWindow>? OnMinimizeRequested;
    public event Action<OsWindow>? OnFocusRequested;
    public event Action<OsWindow, OsDragMode, Vector2>? OnDragRequested;
    public event Action<OsWindow>? OnChanged;

    public event Action<Vector2>? OnPointerMoved;

    public event Action? OnPointerUp;

    private readonly Label _title;
    private readonly OsIconControl _icon;

    private Vector2 _restorePosition;
    private Vector2 _restoreSize;

    private IRenderTexture? _snapshot;

    public OsWindow(ADTOsAppPrototype proto, OsAppControl app, Color accent)
    {
        Proto = proto;
        App = app;
        Title = Loc.GetString(proto.Name);

        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;

        WindowSize = Vector2.Max(proto.DefaultSize, proto.MinSize);

        _icon = new OsIconControl(proto.Icon, 16f)
        {
            Accent = accent,
            Margin = new Thickness(10f, 0f, 8f, 0f),
            VerticalAlignment = VAlignment.Center,
        };

        _title = new Label
        {
            Text = Title,
            HorizontalExpand = true,
            ClipText = true,
            Modulate = OsStyle.Text,
            VerticalAlignment = VAlignment.Center,
        };

        var header = new OsWindowHeader(this)
        {
            MinHeight = OsStyle.TitleBarHeight,
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
        };

        header.AddChild(_icon);
        header.AddChild(_title);
        header.AddChild(MakeCaptionButton(OsCaptionKind.Minimize));
        header.AddChild(MakeCaptionButton(OsCaptionKind.Maximize));
        header.AddChild(MakeCaptionButton(OsCaptionKind.Close));

        App.HorizontalExpand = true;
        App.VerticalExpand = true;
        App.TitleChanged += SetTitle;

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(1f),
            Children =
            {
                header,
                App,
            },
        });
    }

    private OsCaptionButton MakeCaptionButton(OsCaptionKind kind)
    {
        var button = new OsCaptionButton(this, kind);

        button.OnPressed += () => OnCaptionPressed(kind);

        return button;
    }

    private void OnCaptionPressed(OsCaptionKind kind)
    {
        switch (kind)
        {
            case OsCaptionKind.Minimize:
                OnMinimizeRequested?.Invoke(this);
                break;

            case OsCaptionKind.Maximize:
                ToggleMaximized();
                break;

            case OsCaptionKind.Close:
                OnCloseRequested?.Invoke(this);
                break;
        }
    }

    public void RaiseFocus()
    {
        OnFocusRequested?.Invoke(this);
    }

    public void RaiseDrag(OsDragMode mode, Vector2 screen)
    {
        OnDragRequested?.Invoke(this, mode, screen);
    }

    public void SetTitle(string title)
    {
        Title = title;
        _title.Text = title;

        OnChanged?.Invoke(this);
    }

    public void SetAccent(Color accent)
    {
        _icon.Accent = accent;
    }

    public UIBox2 ScreenRect => UIBox2.FromDimensions(GlobalPixelPosition, PixelSize);

    public void ToggleMaximized()
    {
        var from = ScreenRect;

        if (Maximized)
        {
            Maximized = false;
            WindowPosition = _restorePosition;
            WindowSize = _restoreSize;
        }
        else
        {
            Maximized = true;
            _restorePosition = WindowPosition;
            _restoreSize = WindowSize;
        }

        Play(OsWindowAnimation.Resize, from);

        OnChanged?.Invoke(this);
        Parent?.InvalidateArrange();
    }

    public void BeginOpen()
    {
        Play(OsWindowAnimation.Open, default);
    }

    public void BeginClose()
    {
        Closing = true;
        Play(OsWindowAnimation.Close, default);
    }

    public void BeginMinimize(UIBox2 anchor)
    {
        Minimized = true;
        Active = false;
        Play(OsWindowAnimation.Minimize, anchor);
    }

    public void BeginRestore(UIBox2 anchor)
    {
        Minimized = false;
        Visible = true;
        Play(OsWindowAnimation.Restore, anchor);
    }

    public void SetMinimizedInstant(bool minimized)
    {
        Minimized = minimized;
        Visible = !minimized;
    }

    public void SetMaximizedInstant(bool maximized, Vector2 restorePosition, Vector2 restoreSize)
    {
        Maximized = maximized;
        _restorePosition = restorePosition;
        _restoreSize = restoreSize;
    }

    public Vector2 RestorePosition => Maximized ? _restorePosition : WindowPosition;

    public Vector2 RestoreSize => Maximized ? _restoreSize : WindowSize;

    private void Play(OsWindowAnimation animation, UIBox2 anchor)
    {
        AnimationAnchor = anchor;

        if (!Animate)
        {
            Complete(animation);
            return;
        }

        Animation = animation;
        AnimationProgress = 0f;
    }

    private static float Duration(OsWindowAnimation animation)
    {
        return animation switch
        {
            OsWindowAnimation.Open => 0.22f,
            OsWindowAnimation.Close => 0.16f,
            OsWindowAnimation.Minimize => 0.24f,
            OsWindowAnimation.Restore => 0.24f,
            OsWindowAnimation.Resize => 0.2f,
            _ => 0f,
        };
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Animation == OsWindowAnimation.None)
            return;

        AnimationProgress += args.DeltaSeconds / Duration(Animation);

        if (AnimationProgress >= 1f)
            Complete(Animation);
    }

    private void Complete(OsWindowAnimation animation)
    {
        Animation = OsWindowAnimation.None;
        AnimationProgress = 0f;

        ReleaseSnapshot();

        switch (animation)
        {
            case OsWindowAnimation.Close:
                Visible = false;
                PendingRemoval = true;
                break;

            case OsWindowAnimation.Minimize:
                Visible = false;
                break;
        }
    }

    public bool TryGetVisual(UIBox2 layout, out UIBox2 visual, out float alpha)
    {
        var t = AnimationProgress;

        switch (Animation)
        {
            case OsWindowAnimation.Open:
            {
                var eased = OsDraw.EaseOutCubic(t);

                visual = OsDraw.ScaleAround(layout, MathHelper.Lerp(0.86f, 1f, eased));
                alpha = eased;
                return true;
            }

            case OsWindowAnimation.Close:
            {
                var eased = OsDraw.EaseInCubic(t);

                visual = OsDraw.ScaleAround(layout, MathHelper.Lerp(1f, 0.9f, eased));
                alpha = 1f - eased;
                return true;
            }

            case OsWindowAnimation.Minimize:
            {
                var eased = OsDraw.EaseInCubic(t);

                visual = OsDraw.Lerp(layout, AnchorOr(layout), eased);
                alpha = 1f - eased * 0.8f;
                return true;
            }

            case OsWindowAnimation.Restore:
            {
                var eased = OsDraw.EaseOutCubic(t);

                visual = OsDraw.Lerp(AnchorOr(layout), layout, eased);
                alpha = 0.2f + eased * 0.8f;
                return true;
            }

            case OsWindowAnimation.Resize:
            {
                var eased = OsDraw.EaseOutCubic(t);

                visual = OsDraw.Lerp(AnchorOr(layout), layout, eased);
                alpha = 1f;
                return true;
            }
        }

        visual = layout;
        alpha = 1f;
        return false;
    }

    private UIBox2 AnchorOr(UIBox2 layout)
    {
        if (AnimationAnchor.Width > 0f && AnimationAnchor.Height > 0f)
            return AnimationAnchor;

        var bottom = new Vector2(layout.Center.X, layout.Bottom + layout.Height * 0.4f);

        return new UIBox2(bottom - new Vector2(60f, 10f), bottom + new Vector2(60f, 10f));
    }

    public IRenderTexture GetSnapshot(IClyde clyde, Vector2i size)
    {
        if (_snapshot != null && _snapshot.Size == size)
            return _snapshot;

        _snapshot?.Dispose();

        _snapshot = clyde.CreateRenderTarget(
            size,
            RenderTargetColorFormat.Rgba8Srgb,
            new TextureSampleParameters
            {
                Filter = true,
            },
            "InconnuOS window");

        return _snapshot;
    }

    private void ReleaseSnapshot()
    {
        _snapshot?.Dispose();
        _snapshot = null;
    }

    public void ClampSize()
    {
        WindowSize = Vector2.Max(WindowSize, Vector2.Max(Proto.MinSize,
            new Vector2(OsStyle.MinWindowWidth, OsStyle.MinWindowHeight)));
    }

    public bool HitGrip(Vector2 local)
    {
        if (Maximized)
            return false;

        return local.X >= Width - OsStyle.ResizeGrip && local.Y >= Height - OsStyle.ResizeGrip;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnFocusRequested?.Invoke(this);

        if (HitGrip(args.RelativePosition))
            OnDragRequested?.Invoke(this, OsDragMode.Resize, args.PointerLocation.Position);

        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        OnPointerMoved?.Invoke(args.GlobalPixelPosition.Position);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
            OnPointerUp?.Invoke();
    }

    public void ForwardPointerMove(Vector2 screen)
    {
        OnPointerMoved?.Invoke(screen);
    }

    public void ForwardPointerUp()
    {
        OnPointerUp?.Invoke();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var radius = Maximized ? 0f : Radius * scale;
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        var border = Active
            ? OsStyle.Mix(OsStyle.WindowBorder, _icon.Accent, 0.55f)
            : OsStyle.WindowBorder;

        OsDraw.Framed(handle, box, radius, scale, border, OsStyle.WindowBody);

        base.Draw(handle);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            ReleaseSnapshot();
    }
}

public sealed class OsWindowHeader : BoxContainer
{
    private readonly OsWindow _window;

    public OsWindowHeader(OsWindow window)
    {
        _window = window;
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _window.RaiseFocus();
        _window.RaiseDrag(OsDragMode.Move, args.PointerLocation.Position);

        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _window.ForwardPointerMove(args.GlobalPixelPosition.Position);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
            _window.ForwardPointerUp();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var radius = _window.Maximized ? 0f : (OsWindow.Radius - 1f) * scale;
        var header = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        OsDraw.RoundedTop(handle, header, radius, _window.Active ? OsStyle.WindowHeader : OsStyle.WindowHeaderIdle);

        if (_window.Active)
            handle.DrawRect(new UIBox2(0f, PixelHeight - scale, PixelWidth, PixelHeight), OsStyle.TaskbarLine);

        base.Draw(handle);
    }
}

public enum OsCaptionKind : byte
{
    Minimize = 0,
    Maximize,
    Close,
}

public sealed class OsCaptionButton : Control
{
    private readonly OsWindow _window;
    private readonly OsCaptionKind _kind;
    private bool _hovered;

    public event Action? OnPressed;

    public OsCaptionButton(OsWindow window, OsCaptionKind kind)
    {
        _window = window;
        _kind = kind;

        MouseFilter = MouseFilterMode.Stop;
        MinSize = new Vector2(OsStyle.CaptionButtonWidth, OsStyle.TitleBarHeight);
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
        base.Draw(handle);

        var scale = UIScale;
        var width = PixelWidth;
        var height = PixelHeight;

        if (_hovered)
        {
            var highlight = _kind == OsCaptionKind.Close
                ? Color.FromHex("#c42b1c")
                : OsStyle.TaskbarButtonHover;

            handle.DrawRect(new UIBox2(0f, 0f, width, height - scale), highlight);
        }

        var center = new Vector2(width / 2f, height / 2f);
        var arm = 5f * scale;
        var color = _hovered && _kind == OsCaptionKind.Close ? Color.White : OsStyle.Text;

        switch (_kind)
        {
            case OsCaptionKind.Minimize:
                handle.DrawRect(new UIBox2(center.X - arm, center.Y, center.X + arm, center.Y + scale), color);
                break;

            case OsCaptionKind.Maximize when _window.Maximized:
                DrawSquare(handle, center + new Vector2(scale * 2f, -scale * 2f), arm - scale * 2f, color);
                DrawSquare(handle, center + new Vector2(-scale, scale), arm - scale * 2f, color);
                break;

            case OsCaptionKind.Maximize:
                DrawSquare(handle, center, arm, color);
                break;

            case OsCaptionKind.Close:
                handle.DrawLine(center - new Vector2(arm, arm), center + new Vector2(arm, arm), color);
                handle.DrawLine(center + new Vector2(arm, -arm), center + new Vector2(-arm, arm), color);
                break;
        }
    }

    private static void DrawSquare(DrawingHandleScreen handle, Vector2 center, float half, Color color)
    {
        var topLeft = new Vector2(center.X - half, center.Y - half);
        var topRight = new Vector2(center.X + half, center.Y - half);
        var bottomLeft = new Vector2(center.X - half, center.Y + half);
        var bottomRight = new Vector2(center.X + half, center.Y + half);

        handle.DrawLine(topLeft, topRight, color);
        handle.DrawLine(topRight, bottomRight, color);
        handle.DrawLine(bottomRight, bottomLeft, color);
        handle.DrawLine(bottomLeft, topLeft, color);
    }
}
