using Content.Client.ADT.LogicCircuit;
using Content.Client.ADT.LogicCircuit.UI;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.LogicCircuit;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class LogicCircuitApp : OsAppControl
{
    private SharedADTLogicCircuitSystem _logic = default!;

    private LogicCanvas _canvas = default!;
    private LogicNodeInspector _inspector = default!;
    private OptionButton _files = default!;
    private LineEdit _prompt = default!;

    private Label _power = default!;
    private Label _status = default!;
    private CheckBox _enabled = default!;
    private Button _apply = default!;
    private Button _revert = default!;
    private Button _save = default!;

    private readonly List<string> _circuitFiles = new();

    private LogicCircuitLayout _applied = new();
    private LogicCircuitLimits _limits;

    private string? _path;
    private bool _dirty;
    private bool _broken;
    private bool _built;

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (!_built)
        {
            Build();

            Context.MessageReceived += OnServerMessage;

            Context.Send(new ADTOsRequestCircuitMessage());
        }

        if (argument != null)
            LoadFile(argument);

        Refresh();
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        RefreshFileList();
        Refresh();
    }

    private sealed record CircuitState(string? Path, LogicCircuitLayout? Draft);

    public override object? SaveState()
    {
        return new CircuitState(_path, _dirty ? _canvas.Layout.Clone() : null);
    }

    public override void LoadState(object state)
    {
        if (state is not CircuitState saved)
            return;

        _path = saved.Path;

        if (saved.Draft != null)
        {
            _canvas.SetLayout(saved.Draft);
            _canvas.ResetView();
            _dirty = true;
        }

        RefreshFileList();
        Refresh();
    }

    private void Build()
    {
        _built = true;
        _logic = Context.Entities.System<ADTLogicCircuitSystem>();

        _canvas = new LogicCanvas(Context.Prototypes, Context.Cache)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _inspector = new LogicNodeInspector();

        var palette = new LogicPalette(Context.Prototypes);

        _files = new OptionButton
        {
            MinWidth = 180f,
            Margin = new Thickness(0f, 0f, 4f, 0f),
        };

        _save = OsWidgets.Small(Loc.GetString("os-circuit-save"));

        var saveAs = OsWidgets.Small(Loc.GetString("os-circuit-save-as"));

        _prompt = new LineEdit
        {
            HorizontalExpand = true,
            Visible = false,
            PlaceHolder = Loc.GetString("os-circuit-prompt"),
            Margin = new Thickness(4f, 0f, 4f, 0f),
        };

        _power = new Label
        {
            Modulate = OsStyle.Text,
            Margin = new Thickness(4f, 0f, 10f, 0f),
        };

        _status = new Label
        {
            Modulate = OsStyle.Error,
            HorizontalExpand = true,
            ClipText = true,
        };

        _enabled = new CheckBox
        {
            Text = Loc.GetString("os-circuit-enabled"),
            Margin = new Thickness(0f, 0f, 8f, 0f),
        };

        _apply = OsWidgets.Small(Loc.GetString("os-circuit-apply"));
        _revert = OsWidgets.Small(Loc.GetString("os-circuit-revert"));

        var fit = OsWidgets.Small(Loc.GetString("os-circuit-fit"));

        var fileBar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 4f, 4f, 2f),
            Children = { _files, _save, saveAs },
        };

        var editBar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 0f, 4f, 2f),
            Children = { _power, _enabled },
        };

        for (var i = 0; i < LogicCircuitStyle.WireColors.Length; i++)
        {
            var index = i;

            var color = new Button
            {
                Text = " ",
                MinWidth = 20f,
                Margin = new Thickness(0f, 0f, 2f, 0f),
                ModulateSelfOverride = LogicCircuitStyle.WireColor(i),
            };

            color.OnPressed += _ => _canvas.WireColorIndex = index;

            editBar.AddChild(color);
        }

        editBar.AddChild(fit);
        editBar.AddChild(_status);
        editBar.AddChild(_revert);
        editBar.AddChild(_apply);

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children =
            {
                fileBar,
                editBar,
                _prompt,
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    VerticalExpand = true,
                    Children = { _canvas, _inspector },
                },
                palette,
            },
        });

        _canvas.OnLayoutChanged += OnEdited;
        _canvas.OnSelectionChanged += node => _inspector.Show(node);

        _inspector.OnConfigChanged += OnEdited;
        _inspector.OnDeleteRequested += node => _canvas.RemoveNode(node);

        palette.OnElementPicked += proto => _canvas.AddNode(proto);

        _apply.OnPressed += _ => Apply();
        _revert.OnPressed += _ => SetLayout(_applied, true);
        _enabled.OnToggled += args => Context.Send(new ADTLogicCircuitSetEnabledMessage(args.Pressed));
        fit.OnPressed += _ => _canvas.ResetView();

        _save.OnPressed += _ => Save();
        saveAs.OnPressed += _ => OpenPrompt();
        _prompt.OnTextEntered += _ => ConfirmPrompt();
        _files.OnItemSelected += OnFilePicked;

        RefreshFileList();
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case ADTOsCircuitStateMessage circuit:
                ApplyState(circuit.State);
                break;

            case ADTLogicCircuitValuesMessage values:
                _canvas.SetValues(_applied, values.PinValues);
                break;
        }
    }

    private void ApplyState(ADTLogicCircuitBuiState state)
    {
        _applied = state.Layout;
        _limits = state.Limits;
        _broken = state.Broken;

        _enabled.Pressed = state.Enabled;
        _inspector.SetPortCount(Math.Max(state.PortCountIn, state.PortCountOut));

        if (!_dirty)
            SetLayout(state.Layout, false);

        Refresh();
    }

    private void Apply()
    {
        Context.Send(new ADTLogicCircuitApplyMessage(_canvas.Layout));

        _dirty = false;

        Refresh();
    }

    private void SetLayout(LogicCircuitLayout layout, bool resetView)
    {
        _canvas.SetLayout(layout.Clone());

        if (resetView)
            _canvas.ResetView();

        _canvas.ClearValues();

        _dirty = false;

        Refresh();
    }

    private void OnEdited()
    {
        _dirty = true;

        Refresh();
    }

    private void RefreshFileList()
    {
        if (!_built)
            return;

        _circuitFiles.Clear();
        _files.Clear();

        _files.AddItem(Loc.GetString("os-circuit-file-none"), 0);

        foreach (var drive in Context.State.Drives)
        {
            foreach (var file in drive.Disk.Files)
            {
                if (file.Kind != OsFileKind.Circuit)
                    continue;

                _circuitFiles.Add(file.Path);
                _files.AddItem(file.Path, _circuitFiles.Count);
            }
        }

        var selected = _path == null ? -1 : _circuitFiles.IndexOf(_path);

        _files.SelectId(selected < 0 ? 0 : selected + 1);
    }

    private void OnFilePicked(OptionButton.ItemSelectedEventArgs args)
    {
        _files.SelectId(args.Id);

        if (args.Id == 0)
        {
            _path = null;
            Refresh();
            return;
        }

        LoadFile(_circuitFiles[args.Id - 1]);
    }

    private void LoadFile(string path)
    {
        if (Context.FindFile(path) is not { Kind: OsFileKind.Circuit } file || file.Circuit == null)
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.BadCircuit, OsPath.GetName(path)));
            return;
        }

        _path = path;

        SetLayout(file.Circuit, true);
        RefreshFileList();
    }

    private void Save()
    {
        if (_path == null)
        {
            OpenPrompt();
            return;
        }

        Context.Send(new ADTOsFileWriteMessage(_path, OsFileKind.Circuit, string.Empty, _canvas.Layout));
    }

    private void OpenPrompt()
    {
        _prompt.Visible = true;
        _prompt.Text = _path == null ? string.Empty : OsPath.GetNameWithoutExtension(_path);

        _prompt.GrabKeyboardFocus();
    }

    private void ConfirmPrompt()
    {
        var name = _prompt.Text.Trim();

        if (!name.EndsWith(OsPath.ExtensionForKind(OsFileKind.Circuit), OsPath.Comparison))
            name += OsPath.ExtensionForKind(OsFileKind.Circuit);

        if (!OsPath.IsValidName(name))
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.InvalidPath, name));
            return;
        }

        var folder = _path == null ? WritableRoot() : OsPath.GetParent(_path);

        _path = OsPath.Combine(folder, name);

        _prompt.Visible = false;
        _prompt.Text = string.Empty;

        Save();
    }

    private string WritableRoot()
    {
        foreach (var drive in Context.State.Drives)
        {
            if (!drive.ReadOnly)
                return OsPath.GetRoot(drive.Letter);
        }

        return "C:/";
    }

    private void Refresh()
    {
        if (!_built)
            return;

        var used = _logic.GetPowerUsed(_canvas.Layout);

        _power.Text = Loc.GetString("os-circuit-power", ("used", used), ("total", _limits.PowerBudget));

        var name = _path == null
            ? Loc.GetString("os-circuit-file-none")
            : OsPath.GetName(_path);

        SetTitle(_dirty
            ? Loc.GetString("os-circuit-title-dirty", ("file", name))
            : Loc.GetString("os-circuit-title", ("file", name)));

        _save.Disabled = _path != null && !Context.IsWritable(_path);

        if (_broken)
        {
            _status.Text = Loc.GetString("os-circuit-broken");
            _apply.Disabled = !_dirty;
            return;
        }

        if (!_logic.TryValidate(_canvas.Layout, _limits, out var error, out var detail))
        {
            _status.Text = LogicCircuitErrors.GetMessage(error, detail);
            _apply.Disabled = true;
            return;
        }

        _status.Text = string.Empty;
        _apply.Disabled = !_dirty;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !_built)
            return;

        Context.MessageReceived -= OnServerMessage;
    }
}
