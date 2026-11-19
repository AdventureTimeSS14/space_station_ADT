using System.Linq;
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
    private readonly CheckBox _globalVisible;
    private readonly Slider _opacity;
    private readonly SliderEntry _opacityEntry;
    private readonly Label _opacityLabel;
    private readonly BoxContainer _opacityRow;
    private readonly List<LayerRow> _rows = new();
    private bool _canMerge = true;
    private bool _canEdit = true;
    private bool _builtHost;
    private bool _syncing;
    private bool _rebuild;
    private (CanvasLayer Layer, bool Delete)? _armed;

    private sealed record LayerRow(CanvasLayer? Layer, Button Select, CheckBox? Eye, ConfirmButton? Hide, ConfirmButton? Delete);

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
        AddChild(DrawingWindow.WrapLabel(Loc.GetString("adt-drawing-layers-shared"), Color.Gray));

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

        _globalVisible = new CheckBox
        {
            Text = Loc.GetString("adt-drawing-layer-global-visible"),
            ToolTip = Loc.GetString("adt-drawing-layer-global-visible-tooltip"),
        };
        _globalVisible.OnToggled += args =>
        {
            if (!_syncing && _document.Layers.Active is { } layer)
                _document.Layers.SetVisible(layer, args.Pressed);
        };
        AddChild(_globalVisible);

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

    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (_canEdit == value)
                return;

            _canEdit = value;
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
        if (_builtHost != _document.Layers.IsHost)
            return false;

        var layers = _document.Layers.Layers;
        if (layers.Count != _rows.Count - 1)
            return false;

        for (var i = 0; i < layers.Count; i++)
        {
            var row = _rows[layers.Count - 1 - i];
            if (row.Layer != layers[i] || (row.Delete != null) != HostControls(layers[i]))
                return false;
        }

        return true;
    }

    private bool HostControls(CanvasLayer layer)
    {
        return !layer.Own && layer.Registered && _document.Layers.IsHost;
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
                if (layer.Own && layer.Name == name)
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
        _builtHost = _document.Layers.IsHost;
        _list.RemoveAllChildren();
        _rows.Clear();

        var layers = _document.Layers.Layers;
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            var select = SelectButton(layer.Name, layer);

            var eye = new CheckBox
            {
                Pressed = !layer.LocalHidden,
                ToolTip = Loc.GetString("adt-drawing-layer-local-visible"),
            };
            eye.OnToggled += args => _document.Layers.SetLocalHidden(layer, !args.Pressed);

            ConfirmButton? hide = null;
            ConfirmButton? delete = null;
            if (HostControls(layer))
            {
                hide = HostButton(layer, false);
                delete = HostButton(layer, true);
            }

            _rows.Add(new LayerRow(layer, select, eye, hide, delete));

            var row = Row(eye, select);
            if (hide != null && delete != null)
            {
                row.AddChild(hide);
                row.AddChild(delete);
            }

            _list.AddChild(row);
        }

        var canvas = SelectButton(Loc.GetString("adt-drawing-layer-canvas"), null);
        _rows.Add(new LayerRow(null, canvas, null, null, null));
        _list.AddChild(canvas);

        UpdateStates();
    }

    private ConfirmButton HostButton(CanvasLayer layer, bool delete)
    {
        var button = new ConfirmButton
        {
            ConfirmationText = Loc.GetString("adt-canvas-session-confirm"),
            ToolTip = Loc.GetString(delete ? "adt-drawing-layer-host-delete-tooltip" : "adt-drawing-layer-host-hide-tooltip"),
        };
        button.OnPressed += _ => OnHostPressed(layer, delete);
        return button;
    }

    private void OnHostPressed(CanvasLayer layer, bool delete)
    {
        if (!_document.Layers.IsHost)
            return;

        if (_armed is not { } armed || armed.Layer != layer || armed.Delete != delete)
        {
            _armed = (layer, delete);
            UpdateStates();
            return;
        }

        _armed = null;
        if (delete)
            _document.Layers.Remove(layer);
        else
            _document.Layers.SetVisible(layer, !layer.Visible);

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

    private string RowText(CanvasLayer layer)
    {
        var text = layer.Own
            ? layer.Name
            : Loc.GetString("adt-drawing-layer-foreign", ("name", layer.Name), ("owner", layer.OwnerName));

        if (!layer.Visible)
            text = Loc.GetString("adt-drawing-layer-hidden-all", ("name", text));

        return text;
    }

    private void UpdateStates()
    {
        var layers = _document.Layers.Layers;
        var active = _document.Layers.Active;

        if (_armed is { } armed && !layers.Contains(armed.Layer))
            _armed = null;

        foreach (var row in _rows)
        {
            row.Select.Pressed = row.Layer == active;
            if (row.Layer == null)
                continue;

            var layer = row.Layer;
            row.Select.Text = RowText(layer);
            row.Select.Disabled = !layer.Own;
            row.Select.ToolTip = layer.Own ? null : Loc.GetString("adt-drawing-layer-locked");

            if (row.Eye != null)
                row.Eye.Pressed = !layer.LocalHidden;

            if (row.Hide != null)
            {
                var confirming = _armed is { Delete: false } a && a.Layer == layer;
                row.Hide.Text = Loc.GetString(confirming
                    ? "adt-drawing-layer-host-again"
                    : layer.Visible ? "adt-drawing-layer-host-hide" : "adt-drawing-layer-host-show");
            }

            if (row.Delete != null)
            {
                var confirming = _armed is { Delete: true } d && d.Layer == layer;
                row.Delete.Text = Loc.GetString(confirming ? "adt-drawing-layer-host-again" : "adt-drawing-layer-host-delete");
            }
        }

        var index = active == null ? -1 : IndexOf(active);
        _addButton.Disabled = !_canEdit || _document.Layers.OwnCount() >= CanvasLayerStack.MaxLayers;
        _removeButton.Disabled = active == null;
        _upButton.Disabled = active == null || index >= layers.Count - 1;
        _downButton.Disabled = active == null || index <= 0;
        var mergeBlocked = active != null && !_document.CanMergeLayer(active);
        _mergeButton.Disabled = active == null || !_canMerge || mergeBlocked;
        _mergeButton.ToolTip = Loc.GetString(!_canMerge
            ? "adt-drawing-layer-merge-denied"
            : mergeBlocked ? "adt-drawing-layer-merge-blocked" : "adt-drawing-layer-merge-tooltip");

        _syncing = true;
        _globalVisible.Visible = active != null;
        _globalVisible.Pressed = active?.Visible ?? true;
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
