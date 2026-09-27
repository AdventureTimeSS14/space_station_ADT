using System.Numerics;
using Content.Shared.ADT.LogicCircuit;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.LogicCircuit.UI;

public sealed class LogicCircuitWindow : DefaultWindow
{
    private readonly SharedADTLogicCircuitSystem _logic;

    private readonly LogicCanvas _canvas;
    private readonly LogicNodeInspector _inspector;
    private readonly Label _power;
    private readonly Label _status;
    private readonly CheckBox _enabled;
    private readonly Button _apply;
    private readonly Button _revert;
    private LogicCircuitLayout _applied = new();

    private LogicCircuitLimits _limits;
    private bool _dirty;

    public event Action<LogicCircuitLayout>? OnApplyPressed;
    public event Action<bool>? OnEnabledToggled;

    public LogicCircuitWindow(
        IPrototypeManager prototypes,
        IResourceCache cache,
        SharedADTLogicCircuitSystem logic)
    {
        _logic = logic;

        Title = Loc.GetString("logic-circuit-window-title");
        MinSize = new Vector2(900f, 600f);
        SetSize = new Vector2(1100f, 700f);

        _canvas = new LogicCanvas(prototypes, cache);
        _inspector = new LogicNodeInspector();
        _power = new Label { Margin = new Thickness(0f, 0f, 12f, 0f) };
        _status = new Label { Modulate = Color.FromHex("#d07a7a"), HorizontalExpand = true };
        _enabled = new CheckBox { Text = Loc.GetString("logic-circuit-window-enabled") };
        _apply = new Button { Text = Loc.GetString("logic-circuit-window-apply") };
        _revert = new Button { Text = Loc.GetString("logic-circuit-window-revert") };

        var palette = new LogicPalette(prototypes);

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children =
            {
                BuildToolbar(),
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    VerticalExpand = true,
                    Children = { _canvas, _inspector },
                },
                palette,
            },
        });

        _canvas.OnLayoutChanged += OnCanvasChanged;
        _canvas.OnSelectionChanged += node => _inspector.Show(node);

        _inspector.OnConfigChanged += OnCanvasChanged;
        _inspector.OnDeleteRequested += node => _canvas.RemoveNode(node);

        palette.OnElementPicked += proto => _canvas.AddNode(proto);

        _apply.OnPressed += _ => OnApplyPressed?.Invoke(_canvas.Layout);
        _revert.OnPressed += _ => SetLayout(_applied);
        _enabled.OnToggled += args => OnEnabledToggled?.Invoke(args.Pressed);
    }

    private Control BuildToolbar()
    {
        var toolbar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 4f, 4f, 4f),
        };

        toolbar.AddChild(_power);
        toolbar.AddChild(_enabled);

        for (var i = 0; i < LogicCircuitStyle.WireColors.Length; i++)
        {
            var index = i;

            var button = new Button
            {
                Text = " ",
                MinWidth = 22f,
                Margin = new Thickness(2f, 0f, 0f, 0f),
                ModulateSelfOverride = LogicCircuitStyle.WireColor(i),
            };

            button.OnPressed += _ => _canvas.WireColorIndex = index;
            toolbar.AddChild(button);
        }

        var fit = new Button
        {
            Text = Loc.GetString("logic-circuit-window-fit"),
            Margin = new Thickness(12f, 0f, 0f, 0f),
        };

        fit.OnPressed += _ => _canvas.ResetView();

        toolbar.AddChild(fit);
        toolbar.AddChild(_status);
        toolbar.AddChild(_revert);
        toolbar.AddChild(_apply);

        return toolbar;
    }

    public void SetState(ADTLogicCircuitBuiState state)
    {
        _applied = state.Layout;
        _limits = state.Limits;

        _enabled.Pressed = state.Enabled;
        _inspector.SetPortCount(Math.Max(state.PortCountIn, state.PortCountOut));

        SetLayout(state.Layout);

        if (state.Broken)
            _status.Text = Loc.GetString("logic-circuit-window-broken");
    }

    public void SetValues(LogicSignal[] values)
    {
        _canvas.SetValues(_applied, values);
    }

    private void SetLayout(LogicCircuitLayout layout)
    {
        _canvas.SetLayout(layout.Clone());
        _canvas.ResetView();
        _canvas.ClearValues();

        _dirty = false;
        Refresh();
    }

    private void OnCanvasChanged()
    {
        _dirty = true;
        Refresh();
    }

    private void Refresh()
    {
        var used = _logic.GetPowerUsed(_canvas.Layout);
        _power.Text = Loc.GetString("logic-circuit-window-power",
            ("used", used), ("total", _limits.PowerBudget));

        if (!_logic.TryValidate(_canvas.Layout, _limits, out var error, out var detail))
        {
            _status.Text = LogicCircuitErrors.GetMessage(error, detail);
            _apply.Disabled = true;
            return;
        }

        _status.Text = string.Empty;
        _apply.Disabled = !_dirty;
    }
}
