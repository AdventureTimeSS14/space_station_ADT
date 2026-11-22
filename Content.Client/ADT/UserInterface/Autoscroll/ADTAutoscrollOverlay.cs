using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.ADT.UserInterface.Autoscroll;

public sealed class ADTAutoscrollOverlay : Control
{
    private const float DeadZone = 10f;
    private const float SpeedPerPixel = 6f;

    private readonly IUserInterfaceManager _ui;
    private readonly ScrollBar _scrollBar;
    private readonly ScreenCoordinates _anchor;
    private bool _hovered;
    private bool _finishing;

    public event Action? Finished;

    public ADTAutoscrollOverlay(IUserInterfaceManager ui, ScrollBar scrollBar, ScreenCoordinates anchor)
    {
        _ui = ui;
        _scrollBar = scrollBar;
        _anchor = anchor;
        MouseFilter = MouseFilterMode.Stop;
        DefaultCursorShape = CursorShape.Move;
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_finishing)
            return;

        if (_scrollBar.Parent is not { VisibleInTree: true })
        {
            _finishing = true;
            _ui.DeferAction(() => Finished?.Invoke());
            return;
        }

        if (!_hovered && Size != Vector2.Zero)
        {
            _hovered = true;
            _ui.UpdateHovered();
        }

        var mouse = _ui.MousePositionScaled;
        if (mouse.Window != _anchor.Window)
            return;

        var offset = mouse.Position.Y - _anchor.Position.Y;
        var distance = MathF.Abs(offset) - DeadZone;
        if (distance <= 0f)
            return;

        _scrollBar.Value += MathF.Sign(offset) * distance * SpeedPerPixel * args.DeltaSeconds;
    }
}
