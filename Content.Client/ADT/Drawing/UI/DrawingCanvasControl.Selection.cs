using System.Numerics;
using Content.Client.ADT.Drawing.Canvas;
using Robust.Client.Graphics;
using Robust.Client.Input;

namespace Content.Client.ADT.Drawing.UI;

public sealed partial class DrawingCanvasControl
{
    private const int NoHandle = -1;
    private const int MoveHandle = 8;
    private const int MaxLassoPoints = 8192;
    private const float LassoStep = 2f;
    private const float HandleSize = 4f;
    private const float HandleReach = 9f;

    private static readonly Vector2[] Handles =
    {
        new(-1f, -1f),
        new(0f, -1f),
        new(1f, -1f),
        new(1f, 0f),
        new(1f, 1f),
        new(0f, 1f),
        new(-1f, 1f),
        new(-1f, 0f),
    };

    private readonly List<Vector2> _lassoPoints = new();
    private bool _lassoing;
    private CanvasFloating? _floating;
    private int _dragHandle = NoHandle;
    private Vector2 _dragAnchor;
    private Vector2 _dragStart;
    private Vector2 _dragEnd;

    public void ClearSelection()
    {
        CommitFloating();
        _lassoing = false;
        Document.Selection = null;
    }

    public void SelectAll()
    {
        CommitFloating();
        _lassoing = false;
        Document.Selection = CanvasSelection.FromRect(0, 0, Surface.Width, Surface.Height, Surface.Width, Surface.Height);

        if (Tool == DrawingTool.Transform)
            BeginFloating();
    }

    private void BeginLasso(Vector2 screen)
    {
        _lassoing = true;
        _lassoPoints.Clear();
        _lassoPoints.Add(ScreenToCanvas(screen));
    }

    private void AddLassoPoint(Vector2 screen)
    {
        var point = ScreenToCanvas(screen);
        if (_lassoPoints.Count >= MaxLassoPoints ||
            Vector2.Distance(point, _lassoPoints[_lassoPoints.Count - 1]) * Zoom < LassoStep)
        {
            return;
        }

        _lassoPoints.Add(point);
    }

    private void FinishLasso()
    {
        _lassoing = false;
        Document.Selection = CanvasSelection.FromPolygon(_lassoPoints, Surface.Width, Surface.Height);
        _lassoPoints.Clear();
    }

    private bool BeginFloating()
    {
        if (_floating != null)
            return true;

        _floating = Document.BeginFloating();
        if (_floating == null)
            return false;

        Document.Selection = _floating.Selection;
        return true;
    }

    private void CommitFloating()
    {
        if (_floating is not { } floating)
            return;

        ReleaseFloating();
        if (floating.Moved)
            Document.CommitFloating(floating);

        Document.Selection = NextSelection(floating, floating.Moved);
        floating.Dispose();
    }

    private void CancelFloating()
    {
        if (_floating is not { } floating)
            return;

        ReleaseFloating();
        Document.Selection = NextSelection(floating, false);
        floating.Dispose();
    }

    private void ReleaseFloating()
    {
        _floating = null;
        _dragHandle = NoHandle;
    }

    private CanvasSelection? NextSelection(CanvasFloating floating, bool moved)
    {
        if (floating.AutoSelected)
            return null;

        return moved ? floating.Selection.Transformed(floating.Start, floating.End, Surface.Width, Surface.Height) : floating.Selection;
    }

    private void PressTransform(Vector2 screen)
    {
        if (!BeginFloating())
            return;

        var handle = HitFloating(screen);
        if (handle == NoHandle)
        {
            CommitFloating();
            return;
        }

        _dragHandle = handle;
        _dragAnchor = ScreenToCanvas(screen);
        _dragStart = _floating!.Start;
        _dragEnd = _floating.End;
    }

    private int HitFloating(Vector2 screen)
    {
        var floating = _floating!;
        var reach = HandleReach * UIScale;
        for (var i = 0; i < Handles.Length; i++)
        {
            if (Vector2.Distance(screen, HandleScreen(floating, i)) <= reach)
                return i;
        }

        return ScreenBox(CanvasToScreen(floating.Start), CanvasToScreen(floating.End)).Contains(screen) ? MoveHandle : NoHandle;
    }

    private Vector2 HandleScreen(CanvasFloating floating, int handle)
    {
        var t = (Handles[handle] + Vector2.One) / 2f;
        return CanvasToScreen(floating.Start + (floating.End - floating.Start) * t);
    }

    private void DragFloating(Vector2 screen)
    {
        if (_floating is not { } floating)
            return;

        var delta = ScreenToCanvas(screen) - _dragAnchor;
        var start = _dragStart;
        var end = _dragEnd;

        if (_dragHandle == MoveHandle)
        {
            floating.Start = Snap(start + delta);
            floating.End = floating.Start + (end - start);
            return;
        }

        var direction = Handles[_dragHandle];
        if (direction.X < 0f)
            start.X += delta.X;
        else if (direction.X > 0f)
            end.X += delta.X;

        if (direction.Y < 0f)
            start.Y += delta.Y;
        else if (direction.Y > 0f)
            end.Y += delta.Y;

        if (direction.X != 0f && direction.Y != 0f && _inputManager.IsKeyDown(Keyboard.Key.Shift))
            KeepAspect(direction, ref start, ref end);

        start = Snap(start);
        end = Snap(end);

        if (MathF.Abs(end.X - start.X) < 1f)
        {
            if (direction.X < 0f)
                start.X = end.X - 1f;
            else
                end.X = start.X + 1f;
        }

        if (MathF.Abs(end.Y - start.Y) < 1f)
        {
            if (direction.Y < 0f)
                start.Y = end.Y - 1f;
            else
                end.Y = start.Y + 1f;
        }

        floating.Start = start;
        floating.End = end;
    }

    private void KeepAspect(Vector2 direction, ref Vector2 start, ref Vector2 end)
    {
        var original = _dragEnd - _dragStart;
        var size = end - start;
        var scale = MathF.Max(MathF.Abs(size.X / original.X), MathF.Abs(size.Y / original.Y));
        var width = MathF.Abs(original.X) * scale * (size.X < 0f ? -1f : 1f);
        var height = MathF.Abs(original.Y) * scale * (size.Y < 0f ? -1f : 1f);

        if (direction.X < 0f)
            start.X = end.X - width;
        else
            end.X = start.X + width;

        if (direction.Y < 0f)
            start.Y = end.Y - height;
        else
            end.Y = start.Y + height;
    }

    private static Vector2 Snap(Vector2 value)
    {
        return new Vector2(MathF.Round(value.X), MathF.Round(value.Y));
    }

    private void OnLayersChanged()
    {
        if (_floating?.Layer is { } layer && Document.Layers.Find(layer.Id) != layer)
            CancelFloating();
    }

    private void DrawTarget(DrawingHandleScreen handle, PaintSurface surface, Color? modulate = null)
    {
        if (_floating is not { } floating || floating.Target != surface)
        {
            DrawSurface(handle, surface, modulate);
            return;
        }

        DrawSurface(handle, surface, modulate, floating.Hole);
        if (floating.Texture is not { } texture)
            return;

        DrawTextureBetween(handle, texture, CanvasToScreen(floating.Start), CanvasToScreen(floating.End), modulate);
    }

    private void DrawSelection(DrawingHandleScreen handle)
    {
        if (_lassoing)
            DrawOutline(handle, _lassoPoints, _lassoPoints.Count, false);

        if (_floating is { } floating)
        {
            DrawFloatingFrame(handle, floating);
            return;
        }

        if (Document.Selection is { } selection)
            DrawOutline(handle, selection.Polygon, selection.Polygon.Length, true);
    }

    private void DrawOutline(DrawingHandleScreen handle, IList<Vector2> points, int count, bool closed)
    {
        if (count < 2)
            return;

        var last = closed ? count : count - 1;
        for (var i = 0; i < last; i++)
        {
            var from = CanvasToScreen(points[i]);
            var to = CanvasToScreen(points[(i + 1) % count]);
            handle.DrawLine(from + Vector2.One, to + Vector2.One, Color.Black.WithAlpha(0.7f));
            handle.DrawLine(from, to, Color.White);
        }
    }

    private void DrawFloatingFrame(DrawingHandleScreen handle, CanvasFloating floating)
    {
        var box = ScreenBox(CanvasToScreen(floating.Start), CanvasToScreen(floating.End));
        handle.DrawRect(new UIBox2(box.TopLeft + Vector2.One, box.BottomRight + Vector2.One), Color.Black.WithAlpha(0.7f), filled: false);
        handle.DrawRect(box, Color.White, filled: false);

        var size = HandleSize * UIScale;
        for (var i = 0; i < Handles.Length; i++)
        {
            var center = HandleScreen(floating, i);
            var square = new UIBox2(center - new Vector2(size), center + new Vector2(size));
            handle.DrawRect(square, Color.White);
            handle.DrawRect(square, Color.Black, filled: false);
        }
    }
}
