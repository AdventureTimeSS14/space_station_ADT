using System.Numerics;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;

namespace Content.Client.ADT.UserInterface.Autoscroll;

[UsedImplicitly]
public sealed class ADTMiddleClickAutoscrollController : UIController
{
    public const string StyleClassAutoscroll = "ADTAutoscroll";

    private const float DragReleaseDistance = 10f;

    [Dependency] private readonly IInputManager _input = default!;

    private ADTAutoscrollOverlay? _overlay;
    private ScreenCoordinates _anchor;

    public override void Initialize()
    {
        base.Initialize();

        _input.FirstChanceOnKeyEvent += OnKeyEvent;
    }

    private void OnKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (_overlay != null)
        {
            HandleActiveKey(args, type);
            return;
        }

        if (type != KeyEventType.Down || args.Key != Keyboard.Key.MouseMiddle)
            return;

        if (args.Shift || args.Control || args.Alt || args.System)
            return;

        if (FindScrollBar(UIManager.CurrentlyHovered) is not { } scrollBar)
            return;

        if (scrollBar.Root is not WindowRoot root)
            return;

        Start(root, scrollBar);
        args.Handle();
    }

    private void HandleActiveKey(KeyEventArgs args, KeyEventType type)
    {
        if (type == KeyEventType.Up)
        {
            if (args.Key == Keyboard.Key.MouseMiddle && MovedFromAnchor())
                Stop();

            return;
        }

        if (IsModifier(args.Key))
            return;

        Stop();

        if (args.Key.IsMouseKey() || args.Key == Keyboard.Key.Escape)
            args.Handle();
    }

    private void Start(WindowRoot root, ScrollBar scrollBar)
    {
        _anchor = UIManager.MousePositionScaled;
        _overlay = new ADTAutoscrollOverlay(UIManager, scrollBar, _anchor);
        _overlay.Finished += Stop;
        root.PopupRoot.AddChild(_overlay);
    }

    private void Stop()
    {
        if (_overlay == null)
            return;

        var overlay = _overlay;
        _overlay = null;
        overlay.Finished -= Stop;
        overlay.Orphan();
        UIManager.UpdateHovered();
    }

    private bool MovedFromAnchor()
    {
        var mouse = UIManager.MousePositionScaled;
        if (mouse.Window != _anchor.Window)
            return true;

        return Vector2.Distance(mouse.Position, _anchor.Position) > DragReleaseDistance;
    }

    private static bool IsModifier(Keyboard.Key key)
    {
        return key is Keyboard.Key.Shift
            or Keyboard.Key.Control
            or Keyboard.Key.Alt
            or Keyboard.Key.LSystem
            or Keyboard.Key.RSystem;
    }

    private static ScrollBar? FindScrollBar(Control? control)
    {
        ScrollBar? found = null;

        for (var current = control; current != null; current = current.Parent)
        {
            if (found == null && current is ScrollContainer or OutputPanel)
                found = GetScrollableBar(current);

            if (found != null && IsAutoscrollHost(current))
                return found;
        }

        return null;
    }

    private static ScrollBar? GetScrollableBar(Control container)
    {
        foreach (var child in container.Children)
        {
            if (child is VScrollBar bar && bar.MaxValue - bar.MinValue > bar.Page + 1f)
                return bar;
        }

        return null;
    }

    private static bool IsAutoscrollHost(Control control)
    {
        return control is ChatBox or DebugConsole || control.HasStyleClass(StyleClassAutoscroll);
    }
}
