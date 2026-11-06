using System.Linq;
using System.Text;
using Robust.Client.Input;

namespace Content.Client.ADT.Drawing.Input;

public enum DrawingAction : byte
{
    Undo,
    Redo,
    BrushSmaller,
    BrushBigger,
    Brush,
    Eraser,
    Picker,
    Line,
    Rectangle,
    Ellipse,
    Fill,
    Blur,
    Lasso,
    Transform,
    Liquify,
    Mirror,
    SelectAll,
    Deselect,
    RotateCanvas,
    RotateLeft,
    RotateRight,
    ResetRotation,
}

public readonly record struct DrawingKeyBinding(Keyboard.Key Key, bool Ctrl = false, bool Shift = false, bool Alt = false)
{
    public bool IsEmpty => Key == Keyboard.Key.Unknown;

    public bool Matches(KeyEventArgs args)
    {
        return !IsEmpty &&
               args.Key == Key &&
               args.Control == Ctrl &&
               args.Shift == Shift &&
               args.Alt == Alt;
    }

    public string Serialize()
    {
        if (IsEmpty)
            return string.Empty;

        var builder = new StringBuilder();
        if (Ctrl)
            builder.Append("Ctrl+");

        if (Shift)
            builder.Append("Shift+");

        if (Alt)
            builder.Append("Alt+");

        builder.Append(Key.ToString());
        return builder.ToString();
    }

    public string Display(IInputManager input)
    {
        if (IsEmpty)
            return Loc.GetString("adt-drawing-hotkey-none");

        var builder = new StringBuilder();
        if (Ctrl)
            builder.Append("Ctrl+");

        if (Shift)
            builder.Append("Shift+");

        if (Alt)
            builder.Append("Alt+");

        builder.Append(input.GetKeyName(Key));
        return builder.ToString();
    }

    public static bool TryParse(string text, out DrawingKeyBinding binding)
    {
        binding = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var ctrl = false;
        var shift = false;
        var alt = false;
        var parts = text.Split('+');

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i])
            {
                case "Ctrl":
                    ctrl = true;
                    break;
                case "Shift":
                    shift = true;
                    break;
                case "Alt":
                    alt = true;
                    break;
                default:
                    return false;
            }
        }

        if (!Enum.TryParse<Keyboard.Key>(parts[^1], out var key) || key == Keyboard.Key.Unknown)
            return false;

        binding = new DrawingKeyBinding(key, ctrl, shift, alt);
        return true;
    }
}

public sealed class DrawingHotkeyMap
{
    public const int SlotCount = 2;

    private readonly Dictionary<DrawingAction, DrawingKeyBinding[]> _bindings = new();

    public DrawingHotkeyMap()
    {
        ResetToDefaults();
    }

    public event Action? Changed;

    public static IEnumerable<DrawingAction> Actions => Enum.GetValues<DrawingAction>();

    public DrawingKeyBinding Get(DrawingAction action, int slot)
    {
        return _bindings.TryGetValue(action, out var slots) ? slots[slot] : default;
    }

    public void Set(DrawingAction action, int slot, DrawingKeyBinding binding)
    {
        Assign(action, slot, binding);
        Changed?.Invoke();
    }

    private void Assign(DrawingAction action, int slot, DrawingKeyBinding binding)
    {
        if (!binding.IsEmpty)
        {
            foreach (var slots in _bindings.Values)
            {
                for (var i = 0; i < SlotCount; i++)
                {
                    if (slots[i] == binding)
                        slots[i] = default;
                }
            }
        }

        if (!_bindings.TryGetValue(action, out var target))
        {
            target = new DrawingKeyBinding[SlotCount];
            _bindings[action] = target;
        }

        target[slot] = binding;
    }

    public bool TryMatch(KeyEventArgs args, out DrawingAction action)
    {
        foreach (var (candidate, slots) in _bindings)
        {
            foreach (var binding in slots)
            {
                if (!binding.Matches(args))
                    continue;

                action = candidate;
                return true;
            }
        }

        action = default;
        return false;
    }

    public bool UsesKey(DrawingAction action, Keyboard.Key key)
    {
        return _bindings.TryGetValue(action, out var slots) && slots.Any(b => b.Key == key);
    }

    public void ResetToDefaults()
    {
        _bindings.Clear();
        Default(DrawingAction.Undo, new(Keyboard.Key.Z, Ctrl: true));
        Default(DrawingAction.Redo, new(Keyboard.Key.Y, Ctrl: true), new(Keyboard.Key.Z, Ctrl: true, Shift: true));
        Default(DrawingAction.BrushSmaller, new(Keyboard.Key.LBracket));
        Default(DrawingAction.BrushBigger, new(Keyboard.Key.RBracket));
        Default(DrawingAction.Brush, new(Keyboard.Key.E));
        Default(DrawingAction.Eraser);
        Default(DrawingAction.Picker);
        Default(DrawingAction.Line, new(Keyboard.Key.L));
        Default(DrawingAction.Rectangle, new(Keyboard.Key.U));
        Default(DrawingAction.Ellipse, new(Keyboard.Key.O));
        Default(DrawingAction.Fill, new(Keyboard.Key.G));
        Default(DrawingAction.Blur, new(Keyboard.Key.B));
        Default(DrawingAction.Lasso, new(Keyboard.Key.S));
        Default(DrawingAction.Transform, new(Keyboard.Key.T));
        Default(DrawingAction.Liquify, new(Keyboard.Key.W));
        Default(DrawingAction.Mirror, new(Keyboard.Key.M));
        Default(DrawingAction.SelectAll, new(Keyboard.Key.A, Ctrl: true));
        Default(DrawingAction.Deselect, new(Keyboard.Key.D, Ctrl: true));
        Default(DrawingAction.RotateCanvas, new(Keyboard.Key.R));
        Default(DrawingAction.RotateLeft, new(Keyboard.Key.Num4), new(Keyboard.Key.LBracket, Ctrl: true));
        Default(DrawingAction.RotateRight, new(Keyboard.Key.Num6), new(Keyboard.Key.RBracket, Ctrl: true));
        Default(DrawingAction.ResetRotation, new(Keyboard.Key.Num5));
        Changed?.Invoke();
    }

    public DrawingHotkeyData Export()
    {
        var data = new DrawingHotkeyData();
        foreach (var (action, slots) in _bindings)
        {
            data.Hotkeys.Add(new DrawingHotkeyEntry
            {
                Action = action,
                Keys = slots.Select(b => b.Serialize()).ToList(),
            });
        }

        return data;
    }

    public void Import(DrawingHotkeyData data)
    {
        ResetToDefaults();

        foreach (var entry in data.Hotkeys)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                var binding = i < entry.Keys.Count && DrawingKeyBinding.TryParse(entry.Keys[i], out var parsed)
                    ? parsed
                    : default;

                Assign(entry.Action, i, binding);
            }
        }

        Changed?.Invoke();
    }

    private void Default(DrawingAction action, DrawingKeyBinding primary = default, DrawingKeyBinding secondary = default)
    {
        _bindings[action] = new[] { primary, secondary };
    }
}
