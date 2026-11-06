using System.Numerics;
using Content.Client.ADT.Drawing.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Drawing.UI;

public sealed class DrawingHotkeysWindow : DefaultWindow
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private readonly DrawingHotkeyMap _map;
    private readonly Dictionary<(DrawingAction, int), Button> _buttons = new();

    private (DrawingAction Action, int Slot)? _capture;

    public DrawingHotkeysWindow(DrawingHotkeyMap map)
    {
        IoCManager.InjectDependencies(this);
        _map = map;

        Title = Loc.GetString("adt-drawing-hotkeys-title");
        MinSize = new Vector2(460, 420);
        SetSize = new Vector2(520, 640);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };

        root.AddChild(DrawingWindow.WrapLabel(Loc.GetString("adt-drawing-hotkeys-hint"), Color.Gray));

        var list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        foreach (var action in DrawingHotkeyMap.Actions)
        {
            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 4,
            };

            row.AddChild(new Label
            {
                Text = Loc.GetString($"adt-drawing-action-{action.ToString().ToLowerInvariant()}"),
                HorizontalExpand = true,
                ClipText = true,
            });

            for (var slot = 0; slot < DrawingHotkeyMap.SlotCount; slot++)
            {
                var button = new Button
                {
                    MinWidth = 120,
                    ClipText = true,
                };

                var key = (action, slot);
                button.OnPressed += _ => StartCapture(key);
                _buttons[key] = button;
                row.AddChild(button);
            }

            list.AddChild(row);
        }

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
        };
        scroll.AddChild(list);
        root.AddChild(scroll);

        var reset = new Button
        {
            Text = Loc.GetString("adt-drawing-hotkeys-reset"),
        };
        reset.OnPressed += _ =>
        {
            StopCapture();
            _map.ResetToDefaults();
        };
        root.AddChild(reset);

        Contents.AddChild(root);

        _map.Changed += Refresh;
        Refresh();
    }

    protected override void Opened()
    {
        base.Opened();
        _input.FirstChanceOnKeyEvent += OnKeyEvent;
    }

    public override void Close()
    {
        StopCapture();
        _input.FirstChanceOnKeyEvent -= OnKeyEvent;
        _map.Changed -= Refresh;
        base.Close();
    }

    private void StartCapture((DrawingAction, int) key)
    {
        _capture = key;
        Refresh();
    }

    private void StopCapture()
    {
        if (_capture == null)
            return;

        _capture = null;
        Refresh();
    }

    private void OnKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (_capture is not { } capture || type != KeyEventType.Down)
            return;

        if (_ui.KeyboardFocused is { } focused && !IsOwnControl(focused))
        {
            StopCapture();
            return;
        }

        if (args.Key is Keyboard.Key.Control or Keyboard.Key.Shift or Keyboard.Key.Alt or Keyboard.Key.Unknown)
            return;

        if (args.Key.IsMouseKey())
            return;

        args.Handle();

        if (args.Key == Keyboard.Key.Escape)
        {
            StopCapture();
            return;
        }

        var binding = args.Key is Keyboard.Key.BackSpace or Keyboard.Key.Delete
            ? default
            : new DrawingKeyBinding(args.Key, args.Control, args.Shift, args.Alt);

        _capture = null;
        _map.Set(capture.Action, capture.Slot, binding);
    }

    private bool IsOwnControl(Control control)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            if (current == this)
                return true;
        }

        return false;
    }

    private void Refresh()
    {
        foreach (var ((action, slot), button) in _buttons)
        {
            button.Text = _capture is { } capture && capture.Action == action && capture.Slot == slot
                ? Loc.GetString("adt-drawing-hotkey-waiting")
                : _map.Get(action, slot).Display(_input);
        }
    }
}
