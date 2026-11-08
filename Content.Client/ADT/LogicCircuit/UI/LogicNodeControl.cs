using System.Numerics;
using Content.Shared.ADT.LogicCircuit;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.LogicCircuit.UI;

public sealed class LogicNodeControl : Control
{
    private readonly LogicCanvas _canvas;

    public readonly LogicNodeData Node;
    public readonly LogicElementPrototype Proto;

    public bool Selected;

    public int InputCount => Proto.Inputs.Count;
    public int OutputCount => Proto.Outputs.Count;

    public LogicNodeControl(LogicCanvas canvas, LogicNodeData node, LogicElementPrototype proto)
    {
        _canvas = canvas;
        Node = node;
        Proto = proto;

        MouseFilter = MouseFilterMode.Stop;

        RectClipContent = true;

        ToolTip = proto.Description == null ? null : Loc.GetString(proto.Description.Value);
    }

    /// <inheritdoc/>
    public override float UIScale => _canvas.Zoom * (Root?.UIScale ?? 1f);

    public float NodeHeight
    {
        get
        {
            var rows = Math.Max(1, Math.Max(InputCount, OutputCount));
            return LogicCircuitStyle.NodeHeaderHeight
                   + rows * LogicCircuitStyle.NodeRowHeight
                   + LogicCircuitStyle.NodeFooterHeight;
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return new Vector2(LogicCircuitStyle.NodeWidth, NodeHeight);
    }

    public Vector2 GetPinPosition(bool input, int index)
    {
        var y = LogicCircuitStyle.NodeHeaderHeight
                + index * LogicCircuitStyle.NodeRowHeight
                + LogicCircuitStyle.NodeRowHeight / 2f;

        return new Vector2(input ? 0f : LogicCircuitStyle.NodeWidth, y);
    }

    public bool TryGetPinAt(Vector2 local, out bool input, out int index)
    {
        for (var i = 0; i < InputCount; i++)
        {
            if ((local - GetPinPosition(true, i)).Length() > LogicCircuitStyle.PinGrabRadius)
                continue;

            input = true;
            index = i;
            return true;
        }

        for (var i = 0; i < OutputCount; i++)
        {
            if ((local - GetPinPosition(false, i)).Length() > LogicCircuitStyle.PinGrabRadius)
                continue;

            input = false;
            index = i;
            return true;
        }

        input = false;
        index = -1;
        return false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        _canvas.NodeKeyBindDown(this, args);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        _canvas.PointerUp(args);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        _canvas.PointerMove(args.GlobalPixelPosition.Position);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var width = PixelWidth;
        var height = PixelHeight;
        var radius = LogicCircuitStyle.NodeRadius * scale;
        var headerHeight = LogicCircuitStyle.NodeHeaderHeight * scale;

        var body = new UIBox2(0f, 0f, width, height);

        var border = Selected ? LogicCircuitStyle.NodeBorderSelected : LogicCircuitStyle.NodeBorder;
        LogicDraw.RoundedRect(handle, body, radius, border);

        var inset = (Selected ? 2f : 1f) * scale;
        var inner = new UIBox2(inset, inset, width - inset, height - inset);

        LogicDraw.RoundedRect(handle, inner, radius, LogicCircuitStyle.NodeBody);
        LogicDraw.RoundedTop(handle, new UIBox2(inset, inset, width - inset, headerHeight), radius, Proto.Color);

        handle.DrawRect(
            new UIBox2(inset, headerHeight - MathF.Max(1f, scale), width - inset, headerHeight),
            LogicCircuitStyle.HeaderHighlight);

        DrawTitle(handle, scale, width, headerHeight);
        DrawPins(handle, scale, width);
    }

    private void DrawTitle(DrawingHandleScreen handle, float scale, float width, float headerHeight)
    {
        var padding = LogicCircuitStyle.NodePadding * scale;

        LogicDraw.TextInBand(
            handle,
            _canvas.TitleFont,
            new Vector2(padding, 0f),
            headerHeight,
            width - padding * 2f,
            Loc.GetString(Proto.Name),
            scale,
            Color.White);
    }

    private void DrawPins(DrawingHandleScreen handle, float scale, float width)
    {
        var font = _canvas.PinFont;
        var rowHeight = LogicCircuitStyle.NodeRowHeight * scale;
        var pinRadius = LogicCircuitStyle.PinRadius * scale;

        var labelWidth = width / 2f - pinRadius * 2f - LogicCircuitStyle.NodePadding * scale;

        for (var i = 0; i < InputCount; i++)
        {
            var pin = GetPinPosition(true, i) * scale;
            var wired = _canvas.IsInputWired(Node.Id, i);

            DrawPin(handle, pin, pinRadius, wired ? LogicCircuitStyle.PinWired : LogicCircuitStyle.PinIdle);

            LogicDraw.TextInBand(
                handle,
                font,
                new Vector2(pin.X + pinRadius * 2f, pin.Y - rowHeight / 2f),
                rowHeight,
                labelWidth,
                Loc.GetString(Proto.Inputs[i]),
                scale,
                LogicCircuitStyle.TextDim);
        }

        for (var i = 0; i < OutputCount; i++)
        {
            var pin = GetPinPosition(false, i) * scale;
            var value = _canvas.GetPinValue(Node.Id, i);

            DrawPin(handle, pin, pinRadius, value.AsBool()
                ? LogicCircuitStyle.PinActive
                : LogicCircuitStyle.PinIdle);

            var showValue = !value.IsEmpty;
            var label = showValue ? value.AsText() : Loc.GetString(Proto.Outputs[i]);

            LogicDraw.TextInBand(
                handle,
                font,
                new Vector2(pin.X - pinRadius * 2f - labelWidth, pin.Y - rowHeight / 2f),
                rowHeight,
                labelWidth,
                label,
                scale,
                showValue ? LogicCircuitStyle.TextValue : LogicCircuitStyle.TextDim,
                true);
        }
    }

    private static void DrawPin(DrawingHandleScreen handle, Vector2 position, float radius, Color color)
    {
        handle.DrawCircle(position, radius + MathF.Max(1f, radius * 0.35f), LogicCircuitStyle.PinWell);
        handle.DrawCircle(position, radius, color);
    }
}
