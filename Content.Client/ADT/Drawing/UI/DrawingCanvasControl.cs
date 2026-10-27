using System.Numerics;
using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.Input;
using Content.Client.ADT.Drawing.Sync;
using Content.Shared.ADT.Drawing;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Drawing.UI;

public enum DrawingTool : byte
{
    Brush,
    Eraser,
    Picker,
}

public sealed class DrawingCanvasControl : Control
{
    public const float MinBrushSize = 1f;
    public const float MaxBrushSize = 400f;

    private const float MinZoom = 0.05f;
    private const float MaxZoom = 64f;
    private const float PixelGridZoom = 6f;
    private const float BrushSizeStep = 1.15f;
    private const float MaxTaperFloor = 24f;
    private const float MaxTaperPerSize = 6f;
    private const int MaxUploadsPerFrame = 16;

    private static readonly Color BackdropColor = Color.FromHex("#2a2a2e");
    private static readonly Color PaperColor = Color.White;
    private const string CursorFont = "/Fonts/NotoSans/NotoSans-Regular.ttf";

    private readonly IClyde _clyde;
    private readonly IInputManager _inputManager;
    private readonly PenInputProcessor _input;
    private readonly List<Vector2> _arrow = new(3);
    private Font? _font;

    private bool _panning;
    private Vector2 _panStart;
    private Vector2 _panOrigin;
    private Vector2? _hover;
    private bool _fitted;
    private bool _hotkeys;
    private bool _locked;
    private bool _baseLocked;

    public DrawingCanvasControl(CanvasDocument document, PenInputSystem penSystem,
        IGameTiming timing, IClyde clyde, IInputManager inputManager)
    {
        _clyde = clyde;
        _inputManager = inputManager;
        Document = document;
        _input = new PenInputProcessor(penSystem, timing);
        _input.OnSample += OnPenSample;

        RectClipContent = true;
        MouseFilter = MouseFilterMode.Stop;
        CanKeyboardFocus = true;
        KeyboardFocusOnClick = true;
        HorizontalExpand = true;
        VerticalExpand = true;
    }

    public CanvasDocument Document { get; }

    public PaintSurface Surface => Document.Surface;

    public PenInputProcessor Input => _input;

    public BrushSettings Brush => Document.Stroker.Settings;

    public DrawingTool Tool { get; set; } = DrawingTool.Brush;

    public bool HideEditorCursors { get; set; }

    public bool HideViewerCursors { get; set; }

    public bool BaseLocked
    {
        get => _baseLocked;
        set
        {
            _baseLocked = value;
            if (value && Document.Layers.Active == null)
                _input.Cancel();
        }
    }

    public bool CanDrawActive => !_locked && (!_baseLocked || Document.Layers.Active != null);

    public bool Locked
    {
        get => _locked;
        set
        {
            _locked = value;
            if (value)
                _input.Cancel();
        }
    }

    public CanvasCollaboration? Collaboration { get; set; }

    public float Zoom { get; private set; } = 1f;

    public Vector2 Offset { get; private set; }

    public event Action<PenSample>? OnSample;

    public event Action<Color>? OnColorPicked;

    public event Action? OnViewChanged;

    public event Action<float>? OnBrushSizeChanged;

    #region Вид

    public Vector2 ScreenToCanvas(Vector2 controlPixels)
    {
        return (controlPixels - Offset) / Zoom;
    }

    public Vector2 CanvasToScreen(Vector2 canvas)
    {
        return Offset + canvas * Zoom;
    }

    public void FitToView()
    {
        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        var margin = 24f;
        var zx = (PixelSize.X - margin * 2) / Surface.Width;
        var zy = (PixelSize.Y - margin * 2) / Surface.Height;
        Zoom = Math.Clamp(MathF.Min(zx, zy), MinZoom, MaxZoom);
        Offset = ((Vector2) PixelSize - new Vector2(Surface.Width, Surface.Height) * Zoom) / 2f;
        _fitted = true;
        OnViewChanged?.Invoke();
    }

    public void SetZoom(float zoom, Vector2 anchorControlPixels)
    {
        if (!float.IsFinite(zoom))
            return;

        var canvasAnchor = ScreenToCanvas(anchorControlPixels);
        Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        Offset = anchorControlPixels - canvasAnchor * Zoom;
        OnViewChanged?.Invoke();
    }

    public void ChangeBrushSize(float factor)
    {
        if (Brush.Pixel)
        {
            Brush.Size = Math.Clamp(Brush.PixelSize + (factor > 1f ? 1 : -1), 1, BrushSettings.PixelMaxSize);
            OnBrushSizeChanged?.Invoke(Brush.Size);
            return;
        }

        Brush.Size = Math.Clamp(MathF.Round(Brush.Size * factor), MinBrushSize, MaxBrushSize);
        OnBrushSizeChanged?.Invoke(Brush.Size);
    }

    #endregion

    #region Ввод

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _panning = true;
            _panStart = args.RelativePixelPosition;
            _panOrigin = Offset;
            args.Handle();
            return;
        }

        if (!IsDrawFunction(args.Function))
            return;

        args.Handle();
        if (_locked)
            return;

        var screen = _hover ?? args.RelativePixelPosition;

        if (Tool == DrawingTool.Picker)
        {
            var c = ScreenToCanvas(screen);
            var x = (int) MathF.Floor(c.X);
            var y = (int) MathF.Floor(c.Y);
            if (x < 0 || y < 0 || x >= Surface.Width || y >= Surface.Height)
                return;

            var p = Document.ComposedPixel(x, y);
            if (p.A > 0)
                OnColorPicked?.Invoke(new Color(p.R, p.G, p.B, (byte) 255));
            return;
        }

        if (!CanDrawActive)
            return;

        var tool = Tool == DrawingTool.Eraser ? PenTool.Eraser : PenTool.Pen;
        var constant = _inputManager.IsKeyDown(Keyboard.Key.Shift);
        _input.Down(screen, ScreenToCanvas(screen), tool, constant);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _panning = false;
            return;
        }

        if (IsDrawFunction(args.Function) && _input.IsDown)
        {
            var screen = _hover ?? args.RelativePixelPosition;
            _input.Up(screen, ScreenToCanvas(screen));
        }
    }

    protected override void KeyboardFocusEntered()
    {
        base.KeyboardFocusEntered();
        SetHotkeys(true);
    }

    protected override void KeyboardFocusExited()
    {
        base.KeyboardFocusExited();
        SetHotkeys(false);
    }

    private void SetHotkeys(bool enabled)
    {
        if (enabled == _hotkeys)
            return;

        _hotkeys = enabled;
        if (enabled)
            _inputManager.FirstChanceOnKeyEvent += OnKeyEvent;
        else
            _inputManager.FirstChanceOnKeyEvent -= OnKeyEvent;
    }

    private void OnKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (type == KeyEventType.Up)
            return;

        switch (args.Key)
        {
            case Keyboard.Key.Z when args.Control && args.Shift:
            case Keyboard.Key.Y when args.Control:
                if (!_input.IsDown && CanDrawActive)
                    Document.Redo();
                break;

            case Keyboard.Key.Z when args.Control:
                if (!_input.IsDown && CanDrawActive)
                    Document.Undo();
                break;

            case Keyboard.Key.LBracket:
                ChangeBrushSize(1f / BrushSizeStep);
                break;

            case Keyboard.Key.RBracket:
                ChangeBrushSize(BrushSizeStep);
                break;

            default:
                return;
        }

        args.Handle();
    }

    private static bool IsDrawFunction(BoundKeyFunction function)
    {
        return function == EngineKeyFunctions.UIClick || function == EngineKeyFunctions.TextCursorSelect;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        var screen = args.RelativePixelPosition;
        _hover = screen;

        if (_panning)
        {
            Offset = _panOrigin + (screen - _panStart);
            OnViewChanged?.Invoke();
            return;
        }

        if (_input.IsDown && !_inputManager.IsKeyDown(Keyboard.Key.MouseLeft))
            _input.Cancel();

        _input.Move(screen, ScreenToCanvas(screen));
        ReportCursor();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _clyde.OnWindowFocused += OnWindowFocused;
    }

    private void OnWindowFocused(WindowFocusedEventArgs args)
    {
        if (!args.Focused)
            _input.Cancel();
    }

    private void ReportCursor()
    {
        if (Collaboration == null)
            return;

        if (_hover is not { } hover)
        {
            Collaboration.ReportCursor(null, CanvasCursorFlags.None, 0f, Color.White);
            return;
        }

        var flags = Tool switch
        {
            DrawingTool.Eraser => CanvasCursorFlags.Eraser,
            DrawingTool.Picker => CanvasCursorFlags.Picker,
            _ => CanvasCursorFlags.None,
        };

        if (_input.IsDown)
            flags |= CanvasCursorFlags.Drawing;

        var position = _input.IsDown ? _input.Last.Position : ScreenToCanvas(hover);
        var pressure = _input.IsDown ? _input.Last.Pressure : 1f;
        Collaboration.ReportCursor(position, flags, Brush.DiameterAt(pressure), Brush.Color);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (args.Delta.Y == 0f)
            return;

        var factor = MathF.Pow(1.2f, args.Delta.Y);
        SetZoom(Zoom * factor, args.RelativePixelPosition);
        args.Handle();
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hover = null;
        ReportCursor();
    }

    protected override void ControlFocusExited()
    {
        base.ControlFocusExited();
        _panning = false;
        _input.Cancel();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _clyde.OnWindowFocused -= OnWindowFocused;
        SetHotkeys(false);
        _input.Cancel();
        _hover = null;
        ReportCursor();
    }

    private void OnPenSample(PenSample sample)
    {
        var taperOut = Zoom > 0f ? _input.TaperOutLength / Zoom : 0f;
        taperOut = MathF.Min(taperOut, MathF.Max(MaxTaperFloor, Brush.Size * MaxTaperPerSize));
        Document.HandleSample(sample, taperOut);
        OnSample?.Invoke(sample);
    }

    #endregion

    #region Отрисовка

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_fitted)
            FitToView();

        _input.FlushPendingDown();
        Surface.Flush(_clyde, MaxUploadsPerFrame);
        foreach (var layer in Document.Layers.Layers)
        {
            layer.Surface.Flush(_clyde, MaxUploadsPerFrame);
        }

        if (Collaboration != null)
        {
            Collaboration.Advance();
            Collaboration.FlushPreviews(_clyde);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        handle.DrawRect(new UIBox2(Vector2.Zero, PixelSize), BackdropColor);

        var canvasRect = new UIBox2(Offset, CanvasToScreen(new Vector2(Surface.Width, Surface.Height)));
        DrawPaper(handle, canvasRect);

        DrawSurface(handle, Surface);

        if (Collaboration != null)
        {
            foreach (var participant in Collaboration.Participants)
            {
                if (participant.Preview != null)
                    DrawSurface(handle, participant.Preview);
            }
        }

        foreach (var layer in Document.Layers.Layers)
        {
            if (layer.Visible && layer.Opacity > 0f)
                DrawSurface(handle, layer.Surface, Color.White.WithAlpha(layer.Opacity));
        }

        if (Brush.Pixel && Zoom >= PixelGridZoom)
            DrawPixelGrid(handle, canvasRect);

        handle.DrawRect(canvasRect, Color.Black.WithAlpha(0.6f), filled: false);

        if (Collaboration != null)
            DrawCursors(handle, Collaboration);

        if (_hover is not { } hover || Tool == DrawingTool.Picker || _panning || !CanDrawActive)
            return;

        if (Brush.Pixel)
        {
            var size = Brush.PixelSize;
            var cell = ScreenToCanvas(hover);
            var left = MathF.Floor(cell.X) - (size - 1) / 2;
            var top = MathF.Floor(cell.Y) - (size - 1) / 2;
            var box = new UIBox2(CanvasToScreen(new Vector2(left, top)), CanvasToScreen(new Vector2(left + size, top + size)));
            handle.DrawRect(new UIBox2(box.TopLeft - Vector2.One, box.BottomRight + Vector2.One), Color.Black.WithAlpha(0.5f), filled: false);
            handle.DrawRect(box, Color.White.WithAlpha(0.8f), filled: false);
            return;
        }

        var pressure = _input.IsDown ? _input.Last.Pressure : 1f;
        var radius = MathF.Max(1f, Brush.DiameterAt(pressure) * 0.5f * Zoom);
        handle.DrawCircle(hover, radius + 1f, Color.Black.WithAlpha(0.5f), filled: false);
        handle.DrawCircle(hover, radius, Color.White.WithAlpha(0.8f), filled: false);
    }

    private void DrawPixelGrid(DrawingHandleScreen handle, UIBox2 rect)
    {
        var color = Color.Black.WithAlpha(0.15f);
        for (var x = 1; x < Surface.Width; x++)
        {
            var sx = Offset.X + x * Zoom;
            if (sx >= 0f && sx <= PixelSize.X)
                handle.DrawLine(new Vector2(sx, rect.Top), new Vector2(sx, rect.Bottom), color);
        }

        for (var y = 1; y < Surface.Height; y++)
        {
            var sy = Offset.Y + y * Zoom;
            if (sy >= 0f && sy <= PixelSize.Y)
                handle.DrawLine(new Vector2(rect.Left, sy), new Vector2(rect.Right, sy), color);
        }
    }

    private void DrawSurface(DrawingHandleScreen handle, PaintSurface surface, Color? modulate = null)
    {
        foreach (var (rect, texture) in surface.EnumerateTiles())
        {
            if (texture == null)
                continue;

            var screenRect = new UIBox2(CanvasToScreen(rect.TopLeft), CanvasToScreen(rect.BottomRight));
            if (screenRect.Right < 0 || screenRect.Bottom < 0 || screenRect.Left > PixelSize.X || screenRect.Top > PixelSize.Y)
                continue;

            handle.DrawTextureRect(texture, screenRect, modulate);
        }
    }

    private void DrawCursors(DrawingHandleScreen handle, CanvasCollaboration collaboration)
    {
        _font ??= new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>(CursorFont), 11);
        var font = _font;
        var scale = UIScale;

        foreach (var participant in collaboration.Participants)
        {
            if (!participant.CursorVisible)
                continue;

            if (participant.Role == CanvasRole.Viewer ? HideViewerCursors : HideEditorCursors)
                continue;

            var pos = CanvasToScreen(participant.CursorPosition);
            if (pos.X < -64 || pos.Y < -64 || pos.X > PixelSize.X + 64 || pos.Y > PixelSize.Y + 64)
                continue;

            var color = participant.Color;
            var flags = participant.CursorFlags;

            if ((flags & CanvasCursorFlags.Picker) == 0)
            {
                var radius = MathF.Max(2f, participant.CursorSize * 0.5f * Zoom);
                var ring = (flags & CanvasCursorFlags.Eraser) != 0 ? Color.White : participant.BrushColor;
                handle.DrawCircle(pos, radius + 1f, Color.Black.WithAlpha(0.5f), filled: false);
                handle.DrawCircle(pos, radius, ring.WithAlpha(0.9f), filled: false);
            }

            var tip = pos + new Vector2(0f, 16f) * scale;
            var side = pos + new Vector2(11f, 11f) * scale;
            _arrow.Clear();
            _arrow.Add(pos);
            _arrow.Add(tip);
            _arrow.Add(side);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _arrow, color);
            handle.DrawLine(pos, tip, Color.Black);
            handle.DrawLine(tip, side, Color.Black);
            handle.DrawLine(side, pos, Color.Black);

            var textPos = pos + new Vector2(13f, 12f) * scale;
            var textColor = (flags & CanvasCursorFlags.Drawing) != 0 ? color : Color.InterpolateBetween(color, Color.White, 0.35f);
            handle.DrawString(font, textPos + Vector2.One, participant.Name, Color.Black);
            handle.DrawString(font, textPos, participant.Name, textColor);
        }
    }

    private void DrawPaper(DrawingHandleScreen handle, UIBox2 rect)
    {
        if (Clip(rect, new UIBox2(Vector2.Zero, PixelSize)) is { } box)
            handle.DrawRect(box, PaperColor);
    }

    private static UIBox2? Clip(UIBox2 a, UIBox2 b)
    {
        var left = MathF.Max(a.Left, b.Left);
        var top = MathF.Max(a.Top, b.Top);
        var right = MathF.Min(a.Right, b.Right);
        var bottom = MathF.Min(a.Bottom, b.Bottom);
        return right > left && bottom > top ? new UIBox2(left, top, right, bottom) : null;
    }

    #endregion
}
