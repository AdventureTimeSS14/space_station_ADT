using System.Linq;
using System.Numerics;
using Content.Shared.ADT.LogicCircuit;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.LogicCircuit.UI;

public sealed class LogicCanvas : Control
{
    private const int WireSegments = 18;

    private enum DragMode : byte
    {
        None,
        Node,
        Wire,
        Box,
    }

    private readonly IPrototypeManager _prototypes;

    private readonly Dictionary<string, LogicNodeControl> _nodes = new();
    private readonly HashSet<(string Node, int Pin)> _wiredInputs = new();
    private readonly Dictionary<(string Node, int Pin), LogicSignal> _values = new();

    private readonly HashSet<LogicNodeControl> _selection = new();
    private readonly Dictionary<LogicNodeControl, Vector2> _dragOrigins = new();

    private static LogicCircuitLayout? _clipboard;

    private DragMode _drag;
    private Vector2 _dragScreenStart;

    private bool _panning;
    private Vector2 _panScreenStart;
    private Vector2 _panOrigin;
    private LogicNodeControl? _wireNode;
    private int _wirePin;
    private bool _wireFromInput;
    private Vector2 _pointerScreen;
    private Vector2 _boxStartWorld;

    private readonly Vector2[] _curvePoints = new Vector2[WireSegments + 1];
    private readonly Vector2[] _curveVertices = new Vector2[WireSegments * LogicDraw.VerticesPerSegment];

    public readonly Font TitleFont;
    public readonly Font PinFont;

    public float Zoom { get; private set; } = 1f;
    public Vector2 Offset { get; private set; }

    public LogicCircuitLayout Layout { get; private set; } = new();

    public LogicNodeControl? SelectedNode => _selection.Count == 1 ? _selection.First() : null;

    public int WireColorIndex;

    public event Action? OnLayoutChanged;
    public event Action<LogicNodeControl?>? OnSelectionChanged;

    public LogicCanvas(IPrototypeManager prototypes, IResourceCache cache)
    {
        _prototypes = prototypes;
        TitleFont = LogicCircuitStyle.CreateBoldFont(cache, 11);
        PinFont = LogicCircuitStyle.CreateFont(cache, 9);

        RectClipContent = true;
        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;
        VerticalExpand = true;
    }

    #region Схема
    public void SetLayout(LogicCircuitLayout layout)
    {
        Layout = layout;

        RemoveAllChildren();
        _nodes.Clear();
        _selection.Clear();

        foreach (var node in Layout.Nodes)
        {
            AddNodeControl(node);
        }

        RebuildWiredInputs();
        OnSelectionChanged?.Invoke(null);
        InvalidateArrange();
    }

    private LogicNodeControl? AddNodeControl(LogicNodeData node)
    {
        if (!_prototypes.TryIndex(node.Proto, out var proto))
            return null;

        var control = new LogicNodeControl(this, node, proto);
        _nodes[node.Id] = control;
        AddChild(control);
        return control;
    }

    public void AddNode(LogicElementPrototype proto)
    {
        var node = MakeNode(proto, ScreenToWorld(GlobalPixelPosition + (Vector2) PixelSize / 2f));

        Layout.Nodes.Add(node);

        var control = AddNodeControl(node);

        if (control != null)
            SetSelection(control);

        NotifyChanged();
    }

    private LogicNodeData MakeNode(LogicElementPrototype proto, Vector2 position)
    {
        var node = new LogicNodeData
        {
            Id = MakeNodeId(proto.ID),
            Proto = proto.ID,
            Position = position,
            State = new LogicSignal[proto.StateSize],
        };

        foreach (var field in proto.Config)
        {
            node.Config.Add(LogicSignal.FromText(field.Default));
        }

        return node;
    }

    public void RemoveNode(LogicNodeControl control)
    {
        RemoveNodeInternal(control);
        OnSelectionChanged?.Invoke(SelectedNode);
        NotifyChanged();
    }

    private void RemoveNodeInternal(LogicNodeControl control)
    {
        Layout.Nodes.Remove(control.Node);
        Layout.Wires.RemoveAll(wire => wire.From == control.Node.Id || wire.To == control.Node.Id);

        _nodes.Remove(control.Node.Id);
        _selection.Remove(control);
        RemoveChild(control);
    }

    public void RemoveSelection()
    {
        if (_selection.Count == 0)
            return;

        foreach (var control in _selection.ToArray())
        {
            RemoveNodeInternal(control);
        }

        OnSelectionChanged?.Invoke(null);
        NotifyChanged();
    }

    private string MakeNodeId(string protoId)
    {
        var prefix = protoId.StartsWith("Logic") ? protoId[5..] : protoId;
        prefix = prefix.ToLowerInvariant();

        for (var i = 1; i < 1000; i++)
        {
            var candidate = $"{prefix}{i}";

            if (!_nodes.ContainsKey(candidate))
                return candidate;
        }

        return Guid.NewGuid().ToString("N")[..8];
    }

    private void RebuildWiredInputs()
    {
        _wiredInputs.Clear();

        foreach (var wire in Layout.Wires)
        {
            _wiredInputs.Add((wire.To, wire.ToPin));
        }
    }

    public bool IsInputWired(string nodeId, int pin)
    {
        return _wiredInputs.Contains((nodeId, pin));
    }

    public LogicSignal GetPinValue(string nodeId, int pin)
    {
        return _values.TryGetValue((nodeId, pin), out var value) ? value : LogicSignal.Empty;
    }

    public void SetValues(LogicCircuitLayout applied, LogicSignal[] pinValues)
    {
        _values.Clear();

        var index = 0;

        foreach (var node in applied.Nodes)
        {
            if (!_prototypes.TryIndex(node.Proto, out var proto))
                continue;

            for (var pin = 0; pin < proto.Outputs.Count; pin++)
            {
                if (index >= pinValues.Length)
                    return;

                _values[(node.Id, pin)] = pinValues[index];
                index++;
            }
        }
    }

    public void ClearValues()
    {
        _values.Clear();
    }

    private void NotifyChanged()
    {
        RebuildWiredInputs();
        InvalidateArrange();
        OnLayoutChanged?.Invoke();
    }

    #endregion

    #region Выделение и буфер обмена

    private void SetSelection(LogicNodeControl? control)
    {
        foreach (var node in _selection)
        {
            node.Selected = false;
        }

        _selection.Clear();

        if (control != null)
        {
            _selection.Add(control);
            control.Selected = true;
        }

        OnSelectionChanged?.Invoke(SelectedNode);
    }

    public void SelectAll()
    {
        foreach (var node in _nodes.Values)
        {
            _selection.Add(node);
            node.Selected = true;
        }

        OnSelectionChanged?.Invoke(SelectedNode);
    }

    public void CopySelection()
    {
        if (_selection.Count == 0)
            return;

        var ids = new HashSet<string>();
        var origin = new Vector2(float.MaxValue, float.MaxValue);

        foreach (var control in _selection)
        {
            ids.Add(control.Node.Id);
            origin = Vector2.Min(origin, control.Node.Position);
        }

        var copy = new LogicCircuitLayout();

        foreach (var control in _selection)
        {
            var node = control.Node.Clone();
            node.Position -= origin;
            copy.Nodes.Add(node);
        }

        foreach (var wire in Layout.Wires)
        {
            if (ids.Contains(wire.From) && ids.Contains(wire.To))
                copy.Wires.Add(wire.Clone());
        }

        _clipboard = copy;
    }

    public void PasteClipboard()
    {
        if (_clipboard == null || _clipboard.Nodes.Count == 0)
            return;

        var target = ScreenToWorld(_pointerScreen);
        var renames = new Dictionary<string, string>();
        var added = new List<LogicNodeControl>();

        foreach (var source in _clipboard.Nodes)
        {
            if (!_prototypes.TryIndex(source.Proto, out var proto))
                continue;

            var node = source.Clone();
            node.Id = MakeNodeId(proto.ID);
            node.Position = source.Position + target;

            renames[source.Id] = node.Id;
            Layout.Nodes.Add(node);

            var control = AddNodeControl(node);

            if (control != null)
                added.Add(control);
        }

        foreach (var wire in _clipboard.Wires)
        {
            if (!renames.TryGetValue(wire.From, out var from) || !renames.TryGetValue(wire.To, out var to))
                continue;

            var copy = wire.Clone();
            copy.From = from;
            copy.To = to;
            Layout.Wires.Add(copy);
        }

        SetSelection(null);

        foreach (var control in added)
        {
            _selection.Add(control);
            control.Selected = true;
        }

        OnSelectionChanged?.Invoke(SelectedNode);
        NotifyChanged();
    }

    #endregion

    #region Координаты

    private float ScreenScale => Zoom * UIScale;

    public Vector2 ScreenToWorld(Vector2 screen)
    {
        return Offset + (screen - GlobalPixelPosition) / ScreenScale;
    }

    private Vector2 WorldToLocalPixel(Vector2 world)
    {
        return (world - Offset) * ScreenScale;
    }

    public void ResetView()
    {
        Zoom = 1f;

        if (Layout.Nodes.Count == 0)
        {
            Offset = Vector2.Zero;
            InvalidateArrange();
            return;
        }

        var min = Layout.Nodes[0].Position;
        var max = min;

        foreach (var node in Layout.Nodes)
        {
            min = Vector2.Min(min, node.Position);
            max = Vector2.Max(max, node.Position);
        }

        var center = (min + max) / 2f;
        Offset = center - (Vector2) PixelSize / (2f * ScreenScale);
        InvalidateArrange();
    }

    #endregion

    #region Ввод

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        _pointerScreen = args.PointerLocation.Position;

        if (HandleShortcut(args))
            return;

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            if (TryRemoveWireAt(args.PointerLocation.Position))
            {
                args.Handle();
                return;
            }

            _panning = true;
            _panScreenStart = args.PointerLocation.Position;
            _panOrigin = Offset;
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        SetSelection(null);

        _drag = DragMode.Box;
        _boxStartWorld = ScreenToWorld(args.PointerLocation.Position);
        args.Handle();
    }

    private bool HandleShortcut(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.TextDelete)
        {
            RemoveSelection();
            args.Handle();
            return true;
        }

        if (args.Function == EngineKeyFunctions.TextCopy)
        {
            CopySelection();
            args.Handle();
            return true;
        }

        if (args.Function == EngineKeyFunctions.TextPaste)
        {
            PasteClipboard();
            args.Handle();
            return true;
        }

        if (args.Function == EngineKeyFunctions.TextSelectAll)
        {
            SelectAll();
            args.Handle();
            return true;
        }

        return false;
    }

    public void NodeKeyBindDown(LogicNodeControl control, GUIBoundKeyEventArgs args)
    {
        _pointerScreen = args.PointerLocation.Position;

        if (HandleShortcut(args))
            return;

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            RemoveNode(control);
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        var local = (args.PointerLocation.Position - control.GlobalPixelPosition) / ScreenScale;

        if (control.TryGetPinAt(local, out var input, out var pin))
        {
            SetSelection(control);

            _drag = DragMode.Wire;
            _wireNode = control;
            _wirePin = pin;
            _wireFromInput = input;
            args.Handle();
            return;
        }

        if (!_selection.Contains(control))
            SetSelection(control);

        _drag = DragMode.Node;
        _dragScreenStart = args.PointerLocation.Position;

        _dragOrigins.Clear();

        foreach (var node in _selection)
        {
            _dragOrigins[node] = node.Node.Position;
        }

        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        PointerMove(args.GlobalPixelPosition.Position);
    }

    public void PointerMove(Vector2 screen)
    {
        _pointerScreen = screen;

        if (_panning)
        {
            Offset = _panOrigin - (screen - _panScreenStart) / ScreenScale;
            InvalidateArrange();
        }

        switch (_drag)
        {
            case DragMode.Node:
                var delta = (screen - _dragScreenStart) / ScreenScale;

                foreach (var (node, origin) in _dragOrigins)
                {
                    node.Node.Position = origin + delta;
                }

                InvalidateArrange();
                break;
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        PointerUp(args);
    }

    public void PointerUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _panning = false;
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        switch (_drag)
        {
            case DragMode.Node:
                NotifyChanged();
                break;

            case DragMode.Wire:
                FinishWire(args.PointerLocation.Position);
                break;

            case DragMode.Box:
                FinishBox(args.PointerLocation.Position);
                break;
        }

        _drag = DragMode.None;
        _dragOrigins.Clear();
        _wireNode = null;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (args.Delta.Y == 0f)
            return;

        var anchor = ScreenToWorld(args.GlobalPixelPosition.Position);
        var factor = args.Delta.Y > 0f ? 1.15f : 1f / 1.15f;

        Zoom = Math.Clamp(Zoom * factor, LogicCircuitStyle.MinZoom, LogicCircuitStyle.MaxZoom);
        Offset = anchor - (args.GlobalPixelPosition.Position - GlobalPixelPosition) / ScreenScale;

        foreach (var node in _nodes.Values)
        {
            node.InvalidateMeasure();
        }

        InvalidateArrange();
        args.Handle();
    }

    private void FinishBox(Vector2 screen)
    {
        var end = ScreenToWorld(screen);
        var min = Vector2.Min(_boxStartWorld, end);
        var max = Vector2.Max(_boxStartWorld, end);

        foreach (var control in _nodes.Values)
        {
            var position = control.Node.Position;
            var far = position + new Vector2(LogicCircuitStyle.NodeWidth, control.NodeHeight);

            if (far.X < min.X || position.X > max.X || far.Y < min.Y || position.Y > max.Y)
                continue;

            _selection.Add(control);
            control.Selected = true;
        }

        OnSelectionChanged?.Invoke(SelectedNode);
    }

    private void FinishWire(Vector2 screen)
    {
        if (_wireNode == null)
            return;

        var target = FindNodeAt(screen);

        if (target == null || target == _wireNode)
            return;

        var local = (screen - target.GlobalPixelPosition) / ScreenScale;

        if (!target.TryGetPinAt(local, out var targetInput, out var targetPin))
            return;

        if (targetInput == _wireFromInput)
            return;

        var wire = _wireFromInput
            ? new LogicWireData
            {
                From = target.Node.Id,
                FromPin = targetPin,
                To = _wireNode.Node.Id,
                ToPin = _wirePin,
                Color = WireColorIndex,
            }
            : new LogicWireData
            {
                From = _wireNode.Node.Id,
                FromPin = _wirePin,
                To = target.Node.Id,
                ToPin = targetPin,
                Color = WireColorIndex,
            };

        Layout.Wires.RemoveAll(existing => existing.To == wire.To && existing.ToPin == wire.ToPin);
        Layout.Wires.Add(wire);

        NotifyChanged();
    }

    private LogicNodeControl? FindNodeAt(Vector2 screen)
    {
        foreach (var node in _nodes.Values)
        {
            var local = screen - node.GlobalPixelPosition;

            if (local.X < 0f || local.Y < 0f || local.X > node.PixelWidth || local.Y > node.PixelHeight)
                continue;

            return node;
        }

        return null;
    }

    private bool TryRemoveWireAt(Vector2 screen)
    {
        var local = screen - GlobalPixelPosition;
        var threshold = LogicCircuitStyle.WireWidth * ScreenScale + 4f * UIScale;

        for (var i = Layout.Wires.Count - 1; i >= 0; i--)
        {
            if (!TryGetWireEnds(Layout.Wires[i], out var from, out var to))
                continue;

            if (DistanceToCurve(from, to, local) > threshold)
                continue;

            Layout.Wires.RemoveAt(i);
            NotifyChanged();
            return true;
        }

        return false;
    }

    #endregion

    #region Отрисовка

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var infinite = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        foreach (var child in Children)
        {
            child.Measure(infinite);
        }

        return Vector2.Zero;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        foreach (var child in Children)
        {
            if (child is not LogicNodeControl node)
                continue;

            node.Arrange(UIBox2.FromDimensions(node.Node.Position - Offset, node.DesiredSize));
        }

        return finalSize;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        handle.DrawRect(PixelSizeBox, LogicCircuitStyle.Background);

        DrawGrid(handle);
        DrawWires(handle);
        DrawWirePreview(handle);
        DrawSelectionBox(handle);
    }

    private void DrawGrid(DrawingHandleScreen handle)
    {
        var step = LogicCircuitStyle.GridStep * ScreenScale;

        if (step < 6f)
            return;

        var startX = -(Offset.X * ScreenScale) % (step * 5f);
        var startY = -(Offset.Y * ScreenScale) % (step * 5f);
        var index = 0;

        for (var x = startX; x < PixelWidth; x += step, index++)
        {
            if (x < 0f)
                continue;

            var color = index % 5 == 0 ? LogicCircuitStyle.GridLineMajor : LogicCircuitStyle.GridLine;
            handle.DrawLine(new Vector2(x, 0f), new Vector2(x, PixelHeight), color);
        }

        index = 0;

        for (var y = startY; y < PixelHeight; y += step, index++)
        {
            if (y < 0f)
                continue;

            var color = index % 5 == 0 ? LogicCircuitStyle.GridLineMajor : LogicCircuitStyle.GridLine;
            handle.DrawLine(new Vector2(0f, y), new Vector2(PixelWidth, y), color);
        }
    }

    private void DrawWires(DrawingHandleScreen handle)
    {
        foreach (var wire in Layout.Wires)
        {
            if (!TryGetWireEnds(wire, out var from, out var to))
                continue;

            var color = LogicCircuitStyle.WireColor(wire.Color);

            if (GetPinValue(wire.From, wire.FromPin).AsBool())
                color = Color.InterpolateBetween(color, LogicCircuitStyle.PinActive, 0.55f);

            DrawCurve(handle, from, to, color);
        }
    }

    private void DrawWirePreview(DrawingHandleScreen handle)
    {
        if (_drag != DragMode.Wire || _wireNode == null)
            return;

        var start = _wireNode.GlobalPixelPosition - GlobalPixelPosition
                    + _wireNode.GetPinPosition(_wireFromInput, _wirePin) * ScreenScale;
        var end = _pointerScreen - GlobalPixelPosition;

        DrawCurve(handle, _wireFromInput ? end : start, _wireFromInput ? start : end,
            LogicCircuitStyle.WirePreview);
    }

    private void DrawSelectionBox(DrawingHandleScreen handle)
    {
        if (_drag != DragMode.Box)
            return;

        var start = WorldToLocalPixel(_boxStartWorld);
        var end = _pointerScreen - GlobalPixelPosition;

        var box = new UIBox2(
            MathF.Min(start.X, end.X),
            MathF.Min(start.Y, end.Y),
            MathF.Max(start.X, end.X),
            MathF.Max(start.Y, end.Y));

        handle.DrawRect(box, LogicCircuitStyle.SelectionFill);
        handle.DrawRect(box, LogicCircuitStyle.NodeBorderSelected, false);
    }

    private bool TryGetWireEnds(LogicWireData wire, out Vector2 from, out Vector2 to)
    {
        from = default;
        to = default;

        if (!_nodes.TryGetValue(wire.From, out var source) || !_nodes.TryGetValue(wire.To, out var sink))
            return false;

        from = WorldToLocalPixel(source.Node.Position + source.GetPinPosition(false, wire.FromPin));
        to = WorldToLocalPixel(sink.Node.Position + sink.GetPinPosition(true, wire.ToPin));
        return true;
    }

    private void DrawCurve(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
    {
        var tension = MathF.Max(30f, MathF.Abs(to.X - from.X) * 0.5f) * ScreenScale;
        var c1 = from + new Vector2(tension, 0f);
        var c2 = to - new Vector2(tension, 0f);

        _curvePoints[0] = from;

        for (var i = 1; i <= WireSegments; i++)
        {
            _curvePoints[i] = Bezier(from, c1, c2, to, i / (float) WireSegments);
        }

        var width = LogicCircuitStyle.WireWidth * ScreenScale;

        LogicDraw.ThickPolyline(handle, _curvePoints, width + 2f * ScreenScale,
            LogicCircuitStyle.WireShadow, _curveVertices);
        LogicDraw.ThickPolyline(handle, _curvePoints, width, color, _curveVertices);
    }

    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        var inv = 1f - t;
        return a * (inv * inv * inv)
               + b * (3f * inv * inv * t)
               + c * (3f * inv * t * t)
               + d * (t * t * t);
    }

    private float DistanceToCurve(Vector2 from, Vector2 to, Vector2 point)
    {
        const int segments = 12;

        var tension = MathF.Max(30f, MathF.Abs(to.X - from.X) * 0.5f) * ScreenScale;
        var c1 = from + new Vector2(tension, 0f);
        var c2 = to - new Vector2(tension, 0f);
        var previous = from;
        var best = float.MaxValue;

        for (var i = 1; i <= segments; i++)
        {
            var current = Bezier(from, c1, c2, to, i / (float) segments);
            best = MathF.Min(best, DistanceToSegment(previous, current, point));
            previous = current;
        }

        return best;
    }

    private static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 point)
    {
        var delta = b - a;
        var lengthSquared = delta.LengthSquared();

        if (lengthSquared <= 0.0001f)
            return (point - a).Length();

        var t = Math.Clamp(Vector2.Dot(point - a, delta) / lengthSquared, 0f, 1f);
        return (point - (a + delta * t)).Length();
    }

    #endregion
}
