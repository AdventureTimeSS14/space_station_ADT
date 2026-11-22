using System.Numerics;
using Content.Client.ADT.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Sponsors.UI;

public sealed class SponsorColorList : BoxContainer
{
    private readonly Button _header;
    private readonly BoxContainer _body;
    private readonly LegacyColorSelectorSliders _selector;
    private readonly BoxContainer _items;
    private readonly Button _add;

    private readonly string _title;
    private readonly List<Color> _colors = new();
    private Color? _editing;

    public SponsorColorList(string title)
    {
        _title = title;

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 2;

        _header = new Button
        {
            ToggleMode = true,
            HorizontalAlignment = HAlignment.Stretch,
        };

        AddChild(_header);

        _selector = new LegacyColorSelectorSliders
        {
            Color = Color.White,
        };

        _add = new Button
        {
            Text = Loc.GetString("adt-sponsor-editor-color-add"),
        };

        _add.OnPressed += _ => AddCurrent();

        _items = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 0,
        };

        _body = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Visible = false,
            Margin = new Thickness(12, 0, 0, 4),
        };

        _body.AddChild(_selector);
        _body.AddChild(_add);
        AddChild(_body);

        AddChild(_items);

        _header.OnToggled += args =>
        {
            _body.Visible = args.Pressed;

            if (!args.Pressed)
                StopEditing();
        };

        UpdateHeader();
    }

    public void SetColors(IEnumerable<Color> colors)
    {
        _editing = null;
        _colors.Clear();
        _colors.AddRange(colors);
        Rebuild();
    }

    public List<Color> GetColors()
    {
        return new List<Color>(_colors);
    }

    private void AddCurrent()
    {
        var color = _selector.Color;

        if (_editing is { } editing)
        {
            var index = _colors.IndexOf(editing);
            _editing = null;

            if (index >= 0)
            {
                if (editing != color && _colors.Contains(color))
                    _colors.RemoveAt(index);
                else
                    _colors[index] = color;

                Rebuild();
                return;
            }
        }

        if (_colors.Contains(color))
        {
            Rebuild();
            return;
        }

        _colors.Add(color);
        Rebuild();
    }

    private void StopEditing()
    {
        if (_editing == null)
            return;

        _editing = null;
        UpdateAddText();
    }

    private void Rebuild()
    {
        _items.RemoveAllChildren();

        foreach (var color in _colors)
        {
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 4,
                Margin = new Thickness(12, 0, 0, 0),
            };

            row.AddChild(new PanelContainer
            {
                MinSize = new Vector2(28, 16),
                PanelOverride = new StyleBoxFlat(color),
            });

            row.AddChild(new Label
            {
                Text = color.ToHex(),
            });

            var target = color;

            var edit = new Button
            {
                Text = Loc.GetString("adt-sponsor-editor-color-edit"),
            };

            edit.OnPressed += _ =>
            {
                _selector.Color = target;
                _header.Pressed = true;
                _body.Visible = true;
                _editing = target;
                UpdateAddText();
            };

            var remove = new Button
            {
                Text = "x",
            };

            remove.OnPressed += _ =>
            {
                if (_editing is { } editing && editing == target)
                    _editing = null;

                _colors.Remove(target);
                Rebuild();
            };

            row.AddChild(edit);
            row.AddChild(remove);
            _items.AddChild(row);
        }

        UpdateHeader();
        UpdateAddText();
    }

    private void UpdateHeader()
    {
        _header.Text = _colors.Count == 0 ? _title : $"{_title}  ({_colors.Count})";
    }

    private void UpdateAddText()
    {
        if (_editing == null)
        {
            _add.Text = Loc.GetString("adt-sponsor-editor-color-add");
            return;
        }

        _add.Text = Loc.GetString("adt-sponsor-editor-color-apply");
    }
}
