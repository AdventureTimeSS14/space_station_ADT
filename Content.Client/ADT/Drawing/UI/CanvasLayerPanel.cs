using Content.Client.ADT.Drawing.Canvas;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Drawing.UI;

public sealed class CanvasLayerPanel : BoxContainer
{
    private readonly CanvasDocument _document;
    private readonly BoxContainer _list;
    private readonly Button _addButton;
    private readonly ConfirmButton _removeButton;
    private readonly Button _upButton;
    private readonly Button _downButton;
    private readonly Button _mergeButton;
    private readonly Slider _opacity;
    private readonly SliderEntry _opacityEntry;
    private readonly Label _opacityLabel;
    private readonly BoxContainer _opacityRow;
    private readonly List<LayerRow> _rows = new();
    private bool _canMerge = true;
    private bool _syncing;
    private bool _rebuild;

    private sealed record LayerRow(CanvasLayer? Layer, Button Select, CheckBox? Visible);

    public CanvasLayerPanel(CanvasDocument document)
    {
        _document = document;
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;

        AddChild(new Label
        {
            Text = Loc.GetString("adt-drawing-layers-header"),
            StyleClasses = { "LabelHeading" },
        });
        AddChild(DrawingWindow.WrapLabel(Loc.GetString("adt-drawing-layers-private"), Color.Gray));

        _list = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };
        AddChild(_list);

        _addButton = ActionButton("adt-drawing-layer-add", () =>
            _document.Layers.Add(Loc.GetString("adt-drawing-layer-name", ("number", NextNumber()))));
        _removeButton = new ConfirmButton
        {
            Text = Loc.GetString("adt-drawing-layer-remove"),
            ConfirmationText = Loc.GetString("adt-canvas-session-confirm"),
            HorizontalExpand = true,
        };
        _removeButton.OnPressed += _ =>
        {
            if (_document.Layers.Active is { } layer)
                _document.Layers.Remove(layer);
        };
        _upButton = ActionButton("adt-drawing-layer-up", () => MoveActive(1));
        _downButton = ActionButton("adt-drawing-layer-down", () => MoveActive(-1));
        _mergeButton = ActionButton("adt-drawing-layer-merge", MergeActive);

        AddChild(Row(_addButton, _removeButton));
        AddChild(Row(_upButton, _downButton));
        AddChild(_mergeButton);

        _opacityLabel = new Label
        {
            MinWidth = 130,
        };
        _opacity = new Slider
        {
            MinValue = 0,
            MaxValue = 100,
            HorizontalExpand = true,
        };
        _opacityEntry = new SliderEntry(_opacity, "0");
        _opacity.OnValueChanged += r =>
        {
            _opacityLabel.Text = Loc.GetString("adt-drawing-layer-opacity", ("value", (int) r.Value));
            _opacityEntry.Sync();
            if (!_syncing && _document.Layers.Active is { } layer)
                _document.Layers.SetOpacity(layer, r.Value / 100f);
        };
        _opacityRow = Row(_opacityLabel, _opacity, _opacityEntry.Edit);
        AddChild(_opacityRow);

        _document.Layers.Changed += OnLayersChanged;
        Rebuild();
    }

    public bool CanMerge
    {
        get => _canMerge;
        set
        {
            if (_canMerge == value)
                return;

            _canMerge = value;
            UpdateStates();
        }
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _document.Layers.Changed -= OnLayersChanged;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_rebuild)
            Rebuild();
    }

    private void OnLayersChanged()
    {
        if (SameStructure())
            UpdateStates();
        else
            _rebuild = true;
    }

    private bool SameStructure()
    {
        var layers = _document.Layers.Layers;
        if (layers.Count != _rows.Count - 1)
            return false;

        for (var i = 0; i < layers.Count; i++)
        {
            if (_rows[layers.Count - 1 - i].Layer != layers[i])
                return false;
        }

        return true;
    }

    private int NextNumber()
    {
        var number = 1;
        while (true)
        {
            var name = Loc.GetString("adt-drawing-layer-name", ("number", number));
            var taken = false;
            foreach (var layer in _document.Layers.Layers)
            {
                if (layer.Name == name)
                {
                    taken = true;
                    break;
                }
            }

            if (!taken)
                return number;

            number++;
        }
    }

    private void MoveActive(int delta)
    {
        if (_document.Layers.Active is { } layer)
            _document.Layers.Move(layer, delta);
    }

    private void MergeActive()
    {
        if (_canMerge && _document.Layers.Active is { } layer)
            _document.MergeLayer(layer);
    }

    private void Rebuild()
    {
        _rebuild = false;
        _list.RemoveAllChildren();
        _rows.Clear();

        var layers = _document.Layers.Layers;
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            var select = SelectButton(layer.Name, layer);

            var visible = new CheckBox
            {
                Pressed = layer.Visible,
                ToolTip = Loc.GetString("adt-drawing-layer-visible"),
            };
            visible.OnToggled += args => _document.Layers.SetVisible(layer, args.Pressed);

            _rows.Add(new LayerRow(layer, select, visible));
            _list.AddChild(Row(visible, select));
        }

        var canvas = SelectButton(Loc.GetString("adt-drawing-layer-canvas"), null);
        _rows.Add(new LayerRow(null, canvas, null));
        _list.AddChild(canvas);

        UpdateStates();
    }

    private Button SelectButton(string text, CanvasLayer? layer)
    {
        var button = new Button
        {
            Text = text,
            ToggleMode = true,
            HorizontalExpand = true,
            ClipText = true,
        };
        button.OnPressed += _ =>
        {
            _document.Layers.Select(layer);
            UpdateStates();
        };
        return button;
    }

    private void UpdateStates()
    {
        var layers = _document.Layers.Layers;
        var active = _document.Layers.Active;

        foreach (var row in _rows)
        {
            row.Select.Pressed = row.Layer == active;
            if (row.Visible != null && row.Layer != null)
                row.Visible.Pressed = row.Layer.Visible;
        }

        var index = active == null ? -1 : IndexOf(active);
        _addButton.Disabled = layers.Count >= CanvasLayerStack.MaxLayers;
        _removeButton.Disabled = active == null;
        _upButton.Disabled = active == null || index >= layers.Count - 1;
        _downButton.Disabled = active == null || index <= 0;
        _mergeButton.Disabled = active == null || !_canMerge;
        _mergeButton.ToolTip = Loc.GetString(_canMerge ? "adt-drawing-layer-merge-tooltip" : "adt-drawing-layer-merge-denied");

        _syncing = true;
        _opacityRow.Visible = active != null;
        _opacity.Value = (active?.Opacity ?? 1f) * 100f;
        _opacityLabel.Text = Loc.GetString("adt-drawing-layer-opacity", ("value", (int) _opacity.Value));
        _opacityEntry.Sync();
        _syncing = false;
    }

    private int IndexOf(CanvasLayer layer)
    {
        var layers = _document.Layers.Layers;
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] == layer)
                return i;
        }

        return -1;
    }

    private static Button ActionButton(string loc, Action action)
    {
        var button = new Button
        {
            Text = Loc.GetString(loc),
            HorizontalExpand = true,
        };
        button.OnPressed += _ => action();
        return button;
    }

    private static BoxContainer Row(params Control[] children)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        foreach (var child in children)
        {
            row.AddChild(child);
        }

        return row;
    }
}
