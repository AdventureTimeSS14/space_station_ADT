using System.IO;
using System.Numerics;
using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.Input;
using Content.Client.ADT.UserData;
using Content.Shared.ADT.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Drawing.UI;

public sealed partial class DrawingWindow : DefaultWindow
{
    public static readonly ResPath ExportDir = ADTUserData.Root / "canvas" / "exports";

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private readonly PenInputSystem _pen;
    private readonly CanvasDocument _document;
    private readonly DrawingCanvasControl _canvas;
    private readonly BrushSettings _brush;
    private readonly RichTextLabel _sessionLabel;
    private readonly PanelContainer _sessionOverlay;

    private readonly ColorSelectorSliders _colorSelector;
    private readonly Button _brushButton;
    private readonly Button _undoButton;
    private readonly Button _redoButton;
    private readonly ProgressBar _pressureBar;
    private readonly Label _pressureLabel;
    private readonly Label _statusLabel;
    private readonly Label _saveLabel;
    private readonly Button _clearButton;
    private readonly BoxContainer? _hostButtons;
    private readonly CanvasLayerPanel _layerPanel;

    private readonly OptionButton _presetButton;
    private readonly CheckBox _pressureSizeCheck;
    private readonly CheckBox _pressureOpacityCheck;
    private readonly SettingSlider _sizeSlider;
    private readonly SettingSlider _slowSlider;
    private readonly SettingSlider _fastSlider;
    private readonly SettingSlider _fastSpeedSlider;
    private readonly SettingSlider _taperInSlider;
    private readonly SettingSlider _taperOutSlider;
    private readonly SettingSlider _smoothingSlider;
    private bool _syncing;

    public DrawingWindow(CanvasDocument document, bool ownsDocument)
    {
        IoCManager.InjectDependencies(this);
        _pen = _systems.GetEntitySystem<PenInputSystem>();

        Title = Loc.GetString("adt-drawing-window-title");
        MinSize = new Vector2(640, 420);
        SetSize = Vector2.Min(new Vector2(1280, 820), _ui.RootControl.Size * 0.92f);

        _document = document;
        _brush = document.Stroker.Settings;
        _canvas = new DrawingCanvasControl(_document, _pen, _timing, _clyde, _inputManager);
        _canvas.OnSample += UpdatePressure;
        _canvas.OnColorPicked += OnColorPicked;
        _canvas.OnViewChanged += UpdateStatus;
        _document.HistoryChanged += UpdateHistoryButtons;
        _document.Layers.Changed += UpdateHistoryButtons;

        var sidebar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinWidth = 280,
            SeparationOverride = 6,
            Margin = new Thickness(0, 0, 8, 0),
        };

        if (!ownsDocument)
        {
            SessionPanel = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 4,
            };
            sidebar.AddChild(SessionPanel);
            sidebar.AddChild(Check("adt-drawing-hide-editor-cursors", false, v => _canvas.HideEditorCursors = v));
            sidebar.AddChild(Check("adt-drawing-hide-viewer-cursors", false, v => _canvas.HideViewerCursors = v));
        }

        var toolGroup = new ButtonGroup(false);
        _brushButton = ToolButton("adt-drawing-tool-brush", toolGroup, DrawingTool.Brush);
        var eraserButton = ToolButton("adt-drawing-tool-eraser", toolGroup, DrawingTool.Eraser);
        var pickerButton = ToolButton("adt-drawing-tool-picker", toolGroup, DrawingTool.Picker);
        var lineButton = ToolButton("adt-drawing-tool-line", toolGroup, DrawingTool.Line);
        var rectangleButton = ToolButton("adt-drawing-tool-rectangle", toolGroup, DrawingTool.Rectangle);
        var ellipseButton = ToolButton("adt-drawing-tool-ellipse", toolGroup, DrawingTool.Ellipse);
        var fillButton = ToolButton("adt-drawing-tool-fill", toolGroup, DrawingTool.Fill);
        _brushButton.Pressed = true;
        sidebar.AddChild(Row(_brushButton, eraserButton, pickerButton));
        sidebar.AddChild(Row(lineButton, rectangleButton, ellipseButton, fillButton));

        var toolButtons = new Dictionary<DrawingTool, Button>
        {
            [DrawingTool.Line] = lineButton,
            [DrawingTool.Rectangle] = rectangleButton,
            [DrawingTool.Ellipse] = ellipseButton,
            [DrawingTool.Fill] = fillButton,
        };
        _canvas.OnToolSelected += tool =>
        {
            if (toolButtons.TryGetValue(tool, out var button))
                button.Pressed = true;
        };

        sidebar.AddChild(Check("adt-drawing-shape-fill", false, v => _canvas.ShapeFill = v));
        sidebar.AddChild(new SettingSlider("adt-drawing-fill-tolerance", 0, 100, _canvas.FillTolerance * 100, "0",
            v => _canvas.FillTolerance = v / 100f).Row);

        _layerPanel = new CanvasLayerPanel(_document);
        sidebar.AddChild(_layerPanel);

        _colorSelector = new ColorSelectorSliders
        {
            Color = _brush.Color,
        };
        _colorSelector.OnColorChanged += c => _brush.Color = c;
        sidebar.AddChild(_colorSelector);

        var pixel = _brush.Pixel;
        _sizeSlider = new SettingSlider("adt-drawing-size", DrawingCanvasControl.MinBrushSize,
            pixel ? BrushSettings.PixelMaxSize : DrawingCanvasControl.MaxBrushSize,
            _brush.Size, "0", v => _brush.Size = pixel ? MathF.Round(v) : v);
        sidebar.AddChild(_sizeSlider.Row);
        _canvas.OnBrushSizeChanged += _sizeSlider.Set;
        sidebar.AddChild(new SettingSlider("adt-drawing-opacity", 1, 100, _brush.Opacity * 100, "0",
            v => _brush.Opacity = v / 100f).Row);
        sidebar.AddChild(new SettingSlider("adt-drawing-hardness", 0, 100, _brush.Hardness * 100, "0",
            v => _brush.Hardness = v / 100f).Row);
        sidebar.AddChild(new SettingSlider("adt-drawing-stabilizer", 0, 60, 0, "0",
            v => _canvas.Input.Stabilization = v).Row);

        sidebar.AddChild(new Label
        {
            Text = Loc.GetString("adt-drawing-pressure-header"),
            StyleClasses = { "LabelHeading" },
        });

        _presetButton = new OptionButton
        {
            HorizontalExpand = true,
        };
        foreach (var preset in Enum.GetValues<PressurePreset>())
        {
            _presetButton.AddItem(Loc.GetString($"adt-drawing-preset-{preset.ToString().ToLowerInvariant()}"), (int) preset);
        }
        _presetButton.OnItemSelected += args => SelectPreset((PressurePreset) args.Id, true);
        var presetLabel = new Label
        {
            Text = Loc.GetString("adt-drawing-preset"),
        };
        sidebar.AddChild(Row(presetLabel, _presetButton));

        _pressureSizeCheck = Check("adt-drawing-pressure-size", _brush.PressureAffectsSize, v =>
        {
            _brush.PressureAffectsSize = v;
            MarkCustom();
        });
        _pressureOpacityCheck = Check("adt-drawing-pressure-opacity", _brush.PressureAffectsOpacity, v =>
        {
            _brush.PressureAffectsOpacity = v;
            MarkCustom();
        });
        sidebar.AddChild(_pressureSizeCheck);
        sidebar.AddChild(_pressureOpacityCheck);
        sidebar.AddChild(new SettingSlider("adt-drawing-min-size", 0, 100, _brush.MinSizeFraction * 100, "0",
            v => _brush.MinSizeFraction = v / 100f).Row);

        var sim = _pen.SimulationTemplate;
        _slowSlider = EmulationSlider("adt-drawing-sim-slow", 0, 100, "0", v => sim.SlowPressure = v / 100f);
        _fastSlider = EmulationSlider("adt-drawing-sim-fast", 0, 100, "0", v => sim.FastPressure = v / 100f);
        _fastSpeedSlider = EmulationSlider("adt-drawing-sim-speed", 0.5f, 12f, "0.0", v => sim.FastSpeed = v);
        _taperInSlider = EmulationSlider("adt-drawing-sim-taper-in", 0, 100, "0", v => sim.TaperInLength = v);
        _taperOutSlider = EmulationSlider("adt-drawing-sim-taper-out", 0, 100, "0", v => sim.TaperOutLength = v);
        _smoothingSlider = EmulationSlider("adt-drawing-sim-smoothing", 0, 95, "0", v => sim.SpeedSmoothing = v / 100f);
        sidebar.AddChild(_slowSlider.Row);
        sidebar.AddChild(_fastSlider.Row);
        sidebar.AddChild(_fastSpeedSlider.Row);
        sidebar.AddChild(_taperInSlider.Row);
        sidebar.AddChild(_taperOutSlider.Row);
        sidebar.AddChild(_smoothingSlider.Row);

        var curve = new OptionButton();
        curve.AddItem(Loc.GetString("adt-drawing-curve-soft"), 0);
        curve.AddItem(Loc.GetString("adt-drawing-curve-linear"), 1);
        curve.AddItem(Loc.GetString("adt-drawing-curve-hard"), 2);
        curve.SelectId(1);
        curve.OnItemSelected += args =>
        {
            curve.SelectId(args.Id);
            _pen.Curve = args.Id switch
            {
                0 => PressureCurve.Soft,
                2 => PressureCurve.Hard,
                _ => PressureCurve.Linear,
            };
        };
        var curveLabel = new Label
        {
            Text = Loc.GetString("adt-drawing-curve"),
        };
        sidebar.AddChild(Row(curveLabel, curve));

        _pressureBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            MinHeight = 10,
            HorizontalExpand = true,
        };
        _pressureLabel = new Label
        {
            FontColorOverride = Color.Gray,
        };
        sidebar.AddChild(_pressureBar);
        sidebar.AddChild(_pressureLabel);
        sidebar.AddChild(WrapLabel(Loc.GetString("adt-drawing-hotkeys"), Color.Gray));

        _undoButton = ActionButton("adt-drawing-undo", _document.Undo);
        _redoButton = ActionButton("adt-drawing-redo", _document.Redo);
        sidebar.AddChild(Row(_undoButton, _redoButton));
        _clearButton = ActionButton("adt-drawing-clear", () => _document.Clear(Color.White));
        sidebar.AddChild(Row(
            _clearButton,
            ActionButton("adt-drawing-fit", _canvas.FitToView)));
        sidebar.AddChild(Row(
            ActionButton("adt-drawing-save", SavePng),
            ActionButton("adt-drawing-open-folder", OpenExportFolder)));

        if (!ownsDocument)
        {
            _hostButtons = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 4,
            };
            _hostButtons.AddChild(ActionButton("adt-drawing-submit", () => SubmitPressed?.Invoke()));
            _hostButtons.AddChild(ActionButton("adt-drawing-finish", () => FinishPressed?.Invoke()));
            sidebar.AddChild(_hostButtons);
        }

        _saveLabel = new Label
        {
            FontColorOverride = Color.Gray,
            ClipText = true,
        };
        sidebar.AddChild(_saveLabel);

        _statusLabel = new Label
        {
            FontColorOverride = Color.Gray,
            ClipText = true,
        };

        _sessionLabel = new RichTextLabel();
        _sessionOverlay = new PanelContainer
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(8),
            MaxWidth = 520,
            MouseFilter = MouseFilterMode.Ignore,
            Visible = false,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Black.WithAlpha(0.75f),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginTopOverride = 4,
                ContentMarginBottomOverride = 4,
            },
            Children = { _sessionLabel },
        };

        var canvasArea = new Control
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        canvasArea.AddChild(_canvas);
        canvasArea.AddChild(_sessionOverlay);

        var right = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        right.AddChild(canvasArea);
        right.AddChild(_statusLabel);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        root.AddChild(new ScrollContainer
        {
            HScrollEnabled = false,
            MinWidth = 300,
            VerticalExpand = true,
            Children = { sidebar },
        });
        root.AddChild(right);
        Contents.AddChild(root);

        if (ownsDocument)
            OnClose += () => _document.Dispose();

        if (!Enum.TryParse<PressurePreset>(_cfg.GetCVar(ADTCCVars.CanvasPressurePreset), out var saved) ||
            saved == PressurePreset.Custom)
        {
            saved = PressurePreset.Liner;
        }

        SelectPreset(saved, false);
        UpdateHistoryButtons();
        UpdatePressure(default);
        UpdateStatus();
    }

    public event Action? FinishPressed;

    public event Action? SubmitPressed;

    public BoxContainer? SessionPanel { get; }

    public DrawingCanvasControl Canvas => _canvas;

    public void SetSessionStatus(string? text, bool locked, bool baseLocked = false)
    {
        _sessionLabel.SetMessage(FormattedMessage.FromUnformatted(text ?? string.Empty), Color.Gold);
        _sessionOverlay.Visible = !string.IsNullOrEmpty(text);
        _canvas.Locked = locked;
        _canvas.BaseLocked = baseLocked;
        _layerPanel.CanMerge = !locked && !baseLocked;
        UpdateHistoryButtons();
    }

    public static RichTextLabel WrapLabel(string text, Color color)
    {
        var label = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        label.SetMessage(FormattedMessage.FromUnformatted(text), color);
        return label;
    }

    public void SetHost(bool host)
    {
        _clearButton.Visible = host;
        if (_hostButtons != null)
            _hostButtons.Visible = host;
    }

    protected override void Opened()
    {
        base.Opened();
        _canvas.GrabKeyboardFocus();
    }

    private void SelectPreset(PressurePreset preset, bool save)
    {
        _presetButton.SelectId((int) preset);
        if (preset == PressurePreset.Custom)
            return;

        PressurePresets.Apply(preset, _pen, _brush);
        SyncPressureControls();

        if (!save)
            return;

        _cfg.SetCVar(ADTCCVars.CanvasPressurePreset, preset.ToString());
        _cfg.SaveToFile();
    }

    private void SyncPressureControls()
    {
        _syncing = true;

        var sim = _pen.SimulationTemplate;
        _pressureSizeCheck.Pressed = _brush.PressureAffectsSize;
        _pressureOpacityCheck.Pressed = _brush.PressureAffectsOpacity;
        _slowSlider.Set(sim.SlowPressure * 100f);
        _fastSlider.Set(sim.FastPressure * 100f);
        _fastSpeedSlider.Set(sim.FastSpeed);
        _taperInSlider.Set(sim.TaperInLength);
        _taperOutSlider.Set(sim.TaperOutLength);
        _smoothingSlider.Set(sim.SpeedSmoothing * 100f);

        _syncing = false;
    }

    private void MarkCustom()
    {
        if (_syncing)
            return;

        _pen.SimulatePressure = true;
        _presetButton.SelectId((int) PressurePreset.Custom);
    }

    private SettingSlider EmulationSlider(string loc, float min, float max, string format, Action<float> setter)
    {
        return new SettingSlider(loc, min, max, min, format, v =>
        {
            setter(v);
            MarkCustom();
        });
    }

    private void OnColorPicked(Color color)
    {
        _brush.Color = color;
        _colorSelector.Color = color;
        _brushButton.Pressed = true;
        _canvas.Tool = DrawingTool.Brush;
    }

    private void UpdatePressure(PenSample sample)
    {
        _pressureBar.Value = sample.Phase is PenPhase.Down or PenPhase.Move ? sample.Pressure : 0f;

        var source = sample.Origin == PressureOrigin.Simulated
            ? Loc.GetString("adt-drawing-source-simulated")
            : Loc.GetString("adt-drawing-source-constant");

        _pressureLabel.Text = Loc.GetString("adt-drawing-pressure-status",
            ("source", source),
            ("pressure", MathF.Round(sample.Pressure * 100)));
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = Loc.GetString("adt-drawing-status",
            ("width", _document.Width),
            ("height", _document.Height),
            ("zoom", MathF.Round(_canvas.Zoom * 100)));
    }

    private void UpdateHistoryButtons()
    {
        _undoButton.Disabled = !_canvas.CanDrawActive || !_document.ActiveSurface.CanUndo;
        _redoButton.Disabled = !_canvas.CanDrawActive || !_document.ActiveSurface.CanRedo;
    }

    private void SavePng()
    {
        _res.UserData.CreateDir(ExportDir);
        var path = ExportDir / $"drawing-{DateTime.Now:yyyyMMdd-HHmmss}.png";

        try
        {
            using var file = _res.UserData.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            _document.Surface.SavePng(file, _document.Layers.Layers);
            _saveLabel.Text = Loc.GetString("adt-drawing-saved", ("path", path.ToString()));
        }
        catch (Exception e)
        {
            Logger.GetSawmill("adt.drawing").Error($"Failed to save drawing: {e}");
            _saveLabel.Text = Loc.GetString("adt-drawing-save-failed");
        }
    }

    private void OpenExportFolder()
    {
        _res.UserData.CreateDir(ExportDir);
        _res.UserData.OpenOsWindow(ExportDir);
    }

    #region Хелперы разметки

    private Button ToolButton(string loc, ButtonGroup group, DrawingTool tool)
    {
        var button = new Button
        {
            Text = Loc.GetString(loc),
            ToggleMode = true,
            Group = group,
            HorizontalExpand = true,
        };
        button.OnPressed += _ => _canvas.Tool = tool;
        return button;
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

    private static CheckBox Check(string loc, bool value, Action<bool> setter)
    {
        var box = new CheckBox
        {
            Text = Loc.GetString(loc),
            Pressed = value,
        };
        box.OnToggled += args => setter(args.Pressed);
        return box;
    }

    private static BoxContainer Row(params Control[] children)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        foreach (var child in children)
        {
            row.AddChild(child);
        }

        return row;
    }

    private sealed class SettingSlider
    {
        public readonly BoxContainer Row;

        private readonly string _loc;
        private readonly string _format;
        private readonly Label _label;
        private readonly Slider _slider;
        private readonly SliderEntry _entry;

        public SettingSlider(string loc, float min, float max, float value, string format, Action<float> setter)
        {
            _loc = loc;
            _format = format;
            _label = new Label
            {
                MinWidth = 130,
            };
            _slider = new Slider
            {
                MinValue = min,
                MaxValue = max,
                Value = value,
                HorizontalExpand = true,
            };
            _entry = new SliderEntry(_slider, format);
            _slider.OnValueChanged += r =>
            {
                UpdateLabel(r.Value);
                _entry.Sync();
                setter(r.Value);
            };

            UpdateLabel(value);
            Row = DrawingWindow.Row(_label, _slider, _entry.Edit);
        }

        public void Set(float value)
        {
            _slider.SetValueWithoutEvent(value);
            UpdateLabel(_slider.Value);
            _entry.Sync();
        }

        private void UpdateLabel(float value)
        {
            _label.Text = Loc.GetString(_loc, ("value", value.ToString(_format)));
        }
    }

    #endregion
}
