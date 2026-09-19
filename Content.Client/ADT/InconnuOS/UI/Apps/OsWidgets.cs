using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public static class OsWidgets
{
    public static Label Caption(string text)
    {
        return new Label
        {
            Text = text,
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(8f, 8f, 8f, 2f),
        };
    }

    public static Button Small(string text)
    {
        return new Button
        {
            Text = text,
            Margin = new Thickness(0f, 0f, 4f, 0f),
        };
    }

    public static OsAppIcon IconFor(OsFile file)
    {
        if (file.IsDirectory)
            return OsAppIcon.Folder;

        return file.Kind switch
        {
            OsFileKind.Circuit => OsAppIcon.Circuit,
            OsFileKind.Shortcut => OsAppIcon.Generic,
            _ => OsAppIcon.Notepad,
        };
    }
}

public sealed class OsInfoRow : BoxContainer
{
    private readonly Label _caption;
    private readonly Label _value;

    public string Caption
    {
        get => _caption.Text ?? string.Empty;
        set => _caption.Text = value;
    }

    public string Value
    {
        get => _value.Text ?? string.Empty;
        set => _value.Text = value;
    }

    public OsInfoRow(string caption, string value)
    {
        Orientation = LayoutOrientation.Horizontal;
        Margin = new Thickness(10f, 1f, 10f, 1f);

        _caption = new Label
        {
            Text = caption,
            Modulate = OsStyle.TextDim,
            MinWidth = 140f,
        };

        _value = new Label
        {
            Text = value,
            Modulate = OsStyle.Text,
            ClipText = true,
            HorizontalExpand = true,
        };

        AddChild(_caption);
        AddChild(_value);
    }
}

public sealed class OsPanel : Control
{
    public OsPanel()
    {
        MouseFilter = MouseFilterMode.Pass;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;

        OsDraw.RoundedRect(handle, new UIBox2(0f, 0f, PixelWidth, PixelHeight), 3f * scale, OsStyle.WindowBorder);
        OsDraw.RoundedRect(handle, new UIBox2(scale, scale, PixelWidth - scale, PixelHeight - scale), 3f * scale, OsStyle.Panel);

        base.Draw(handle);
    }
}

public sealed class OsListRow : Control
{
    private readonly Color _accent;
    private bool _hovered;

    public bool Selected;

    public event Action? OnSelected;
    public event Action? OnActivated;

    public event Action<Vector2>? OnContextMenu;

    public OsListRow(OsAppIcon icon, string text, string? trailing, Color accent)
    {
        _accent = accent;

        MouseFilter = MouseFilterMode.Stop;
        MinHeight = 22f;

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalExpand = true,
        };

        row.AddChild(new OsIconControl(icon, 14f)
        {
            Accent = accent,
            Margin = new Thickness(6f, 0f, 6f, 0f),
            VerticalAlignment = VAlignment.Center,
        });

        row.AddChild(new Label
        {
            Text = text,
            Modulate = OsStyle.Text,
            ClipText = true,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        });

        if (trailing != null)
        {
            row.AddChild(new Label
            {
                Text = trailing,
                Modulate = OsStyle.TextDim,
                VerticalAlignment = VAlignment.Center,
                Margin = new Thickness(0f, 0f, 8f, 0f),
            });
        }

        AddChild(row);
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

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            if (!Selected)
                OnSelected?.Invoke();

            OnContextMenu?.Invoke(args.PointerLocation.Position);
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (Selected)
        {
            OnActivated?.Invoke();
        }
        else
        {
            OnSelected?.Invoke();
        }

        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = UIScale;

        if (Selected)
        {
            handle.DrawRect(new UIBox2(0f, 0f, PixelWidth, PixelHeight), OsStyle.Mix(OsStyle.Panel, _accent, 0.45f));
        }
        else if (_hovered)
        {
            handle.DrawRect(new UIBox2(0f, 0f, PixelWidth, PixelHeight), OsStyle.PanelLight);
        }

        base.Draw(handle);
    }
}

public sealed class OsClickArea : Control
{
    public event Action<Vector2>? OnContextMenu;
    public event Action? OnClicked;

    public OsClickArea()
    {
        MouseFilter = MouseFilterMode.Pass;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            OnContextMenu?.Invoke(args.PointerLocation.Position);
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnClicked?.Invoke();
        args.Handle();
    }
}

public sealed class OsUsageBar : Control
{
    public Color Accent;
    public float Fraction;

    public OsUsageBar(Color accent)
    {
        Accent = accent;

        MouseFilter = MouseFilterMode.Ignore;
        MinSize = new Vector2(120f, 6f);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var scale = UIScale;
        var radius = PixelHeight / 2f;
        var filled = Math.Clamp(Fraction, 0f, 1f);

        OsDraw.RoundedRect(handle, new UIBox2(0f, 0f, PixelWidth, PixelHeight), radius, OsStyle.WindowBody);

        if (filled <= 0f)
            return;

        var color = filled > 0.9f ? OsStyle.Error : Accent;

        OsDraw.RoundedRect(handle, new UIBox2(0f, 0f, MathF.Max(PixelWidth * filled, scale * 2f), PixelHeight), radius, color);
    }
}
