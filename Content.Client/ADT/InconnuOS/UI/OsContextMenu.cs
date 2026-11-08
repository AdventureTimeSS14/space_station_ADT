using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsMenuEntry
{
    public readonly string Text;
    public readonly Action? Action;
    public readonly bool Disabled;

    public bool Separator => Action == null && Text.Length == 0;

    public OsMenuEntry(string text, Action? action, bool disabled = false)
    {
        Text = text;
        Action = action;
        Disabled = disabled;
    }

    public static OsMenuEntry Line()
    {
        return new OsMenuEntry(string.Empty, null);
    }
}

public sealed class OsPopupLayer : Control
{
    private OsContextMenu? _menu;
    private Vector2 _position;

    public OsPopupLayer()
    {
        MouseFilter = MouseFilterMode.Ignore;
    }

    public bool IsOpen => _menu != null;

    public void Show(Vector2 screen, Color accent, List<OsMenuEntry> entries)
    {
        Close();

        if (entries.Count == 0)
            return;

        _menu = new OsContextMenu(accent, entries);
        _menu.OnPicked += Close;

        _position = (screen - GlobalPixelPosition) / UIScale;

        AddChild(_menu);
        MouseFilter = MouseFilterMode.Stop;

        InvalidateArrange();
    }

    public void Close()
    {
        if (_menu == null)
            return;

        RemoveChild(_menu);

        _menu = null;
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (_menu == null)
            return finalSize;

        _menu.Measure(finalSize);

        var size = _menu.DesiredSize;

        var x = _position.X + size.X > finalSize.X ? _position.X - size.X : _position.X;
        var y = _position.Y + size.Y > finalSize.Y ? _position.Y - size.Y : _position.Y;

        x = Math.Clamp(x, 0f, MathF.Max(0f, finalSize.X - size.X));
        y = Math.Clamp(y, 0f, MathF.Max(0f, finalSize.Y - size.Y));

        _menu.Arrange(UIBox2.FromDimensions(new Vector2(x, y), size));

        return finalSize;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        Close();
        args.Handle();
    }
}

public sealed class OsContextMenu : Control
{
    private readonly Color _accent;

    private float _appear;

    public event Action? OnPicked;

    public OsContextMenu(Color accent, List<OsMenuEntry> entries)
    {
        _accent = accent;

        MouseFilter = MouseFilterMode.Stop;
        MinWidth = 190f;

        var rows = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(4f),
        };

        foreach (var entry in entries)
        {
            if (entry.Separator)
            {
                rows.AddChild(new OsMenuSeparator());
                continue;
            }

            var row = new OsContextMenuRow(entry, accent);

            row.OnPressed += () =>
            {
                OnPicked?.Invoke();
                entry.Action?.Invoke();
            };

            rows.AddChild(row);
        }

        AddChild(rows);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_appear >= 1f)
            return;

        _appear = MathF.Min(1f, _appear + args.DeltaSeconds / 0.12f);
        Modulate = Color.White.WithAlpha(OsDraw.EaseOutCubic(_appear));
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        OsDraw.Shadow(handle, box, 5f * scale, 10f * scale, 0.45f * Modulate.A);
        OsDraw.Framed(handle, box, 5f * scale, scale, OsStyle.WindowBorder, OsStyle.Panel);

        base.Draw(handle);
    }
}

public sealed class OsContextMenuRow : Control
{
    private readonly OsMenuEntry _entry;
    private readonly Color _accent;
    private bool _hovered;

    public event Action? OnPressed;

    public OsContextMenuRow(OsMenuEntry entry, Color accent)
    {
        _entry = entry;
        _accent = accent;

        MouseFilter = MouseFilterMode.Stop;
        MinHeight = 24f;

        AddChild(new Label
        {
            Text = entry.Text,
            Modulate = entry.Disabled ? OsStyle.TextDim : OsStyle.Text,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(12f, 0f, 16f, 0f),
        });
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

        args.Handle();

        if (_entry.Disabled)
            return;

        OnPressed?.Invoke();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_hovered && !_entry.Disabled)
        {
            var scale = UIScale;

            OsDraw.RoundedRect(handle, new UIBox2(0f, 0f, PixelWidth, PixelHeight), 3f * scale,
                OsStyle.Mix(OsStyle.Panel, _accent, 0.5f));
        }

        base.Draw(handle);
    }
}

public sealed class OsMenuSeparator : Control
{
    public OsMenuSeparator()
    {
        MinHeight = 9f;
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;
        var y = MathF.Floor(PixelHeight / 2f);

        handle.DrawRect(new UIBox2(6f * scale, y, PixelWidth - 6f * scale, y + scale), OsStyle.WindowBorder);
    }
}
