using System.Collections.Generic;
using Content.Shared.ADT.Shields;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;

namespace Content.Client.ADT.Shields.UI;

public sealed class ShieldGeneratorWindow : DefaultWindow
{
    public event Action<BoundUserInterfaceMessage>? OnSendMessage;

    private readonly Label _status;
    private readonly Label _integrity;
    private readonly Label _energy;
    private readonly Label _segments;
    private readonly Label _upkeep;
    private readonly Label _conduits;
    private readonly Button _toggleButton;
    private readonly Button _emergencyButton;
    private readonly Label _inputCapLabel;
    private readonly SliderIntInput _inputCap;
    private readonly Button _applyInputCap;
    private readonly BoxContainer _modesContainer;
    private readonly Dictionary<ShieldModes, CheckBox> _modeBoxes = new();

    private bool _updating;

    public ShieldGeneratorWindow()
    {
        Title = Loc.GetString("shield-window-title");
        MinSize = new System.Numerics.Vector2(420, 520);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinSize = new System.Numerics.Vector2(400, 480),
        };

        _status = new Label { Text = Loc.GetString("shield-status-off") };
        root.AddChild(_status);

        _integrity = new Label();
        root.AddChild(_integrity);

        _energy = new Label();
        root.AddChild(_energy);

        _upkeep = new Label();
        root.AddChild(_upkeep);

        _segments = new Label();
        root.AddChild(_segments);

        _conduits = new Label();
        root.AddChild(_conduits);

        root.AddChild(new Control { MinHeight = 8 });

        _toggleButton = new Button
        {
            Text = Loc.GetString("shield-window-start"),
            StyleClasses = { "OpenRight" },
        };
        _toggleButton.OnPressed += OnTogglePressed;
        root.AddChild(_toggleButton);

        _emergencyButton = new Button
        {
            Text = Loc.GetString("shield-window-emergency"),
            StyleClasses = { "OpenRight" },
        };
        _emergencyButton.OnPressed += OnEmergencyPressed;
        root.AddChild(_emergencyButton);

        root.AddChild(new Control { MinHeight = 8 });

        _inputCapLabel = new Label { Text = Loc.GetString("shield-window-input-cap") };
        root.AddChild(_inputCapLabel);

        var capRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
        };

        _inputCap = new SliderIntInput
        {
            MinValue = 0,
            MaxValue = 3000,
            Value = 1000,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 1f,
        };
        capRow.AddChild(_inputCap);

        _applyInputCap = new Button
        {
            Text = Loc.GetString("shield-window-apply"),
        };
        _applyInputCap.OnPressed += OnApplyInputCap;
        capRow.AddChild(_applyInputCap);

        root.AddChild(capRow);

        root.AddChild(new Control { MinHeight = 8 });

        _modesContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        root.AddChild(_modesContainer);

        Contents.AddChild(root);
    }

    public void UpdateState(ShieldGeneratorBuiState state)
    {
        _updating = true;

        switch (state.Running)
        {
            case ShieldRunningState.Running:
                _status.Text = Loc.GetString("shield-status-running");
                _toggleButton.Text = Loc.GetString("shield-window-stop");
                break;
            case ShieldRunningState.Discharging:
                _status.Text = Loc.GetString("shield-status-discharging");
                _toggleButton.Text = Loc.GetString("shield-window-stop");
                break;
            default:
                _status.Text = Loc.GetString("shield-status-off");
                _toggleButton.Text = Loc.GetString("shield-window-start");
                break;
        }

        if (state.Overloaded)
            _status.Text = Loc.GetString("shield-status-overloaded");

        _integrity.Text = Loc.GetString("shield-window-integrity", ("integrity", (int) state.FieldIntegrity));
        _energy.Text = Loc.GetString("shield-window-energy", ("current", state.CurrentEnergy), ("max", state.MaxEnergy));
        _upkeep.Text = Loc.GetString("shield-window-upkeep", ("amount", (int) state.CurrentUpkeep));
        _segments.Text = Loc.GetString("shield-window-segments", ("total", state.TotalSegments), ("functional", state.FunctionalSegments));
        _conduits.Text = Loc.GetString("shield-window-conduits", ("deployed", state.ConduitsDeployed), ("required", state.RequiredConduits));

        _toggleButton.Disabled = state.OfflineFor > 0;
        _emergencyButton.Disabled = state.Running == ShieldRunningState.Off;

        _inputCap.MinValue = 0;
        _inputCap.MaxValue = (int) (state.MaxInputCap / 1000f);
        _inputCap.Value = (int) (state.InputCap / 1000f);
        _inputCapLabel.Text = Loc.GetString("shield-window-input-cap-value", ("cap", (int) (state.InputCap / 1000f)));

        if (_modeBoxes.Count == 0)
        {
            foreach (var mode in state.Modes)
            {
                var checkbox = new CheckBox
                {
                    Text = Loc.GetString(mode.Name),
                    ToolTip = Loc.GetString(mode.Description),
                };

                var modeFlag = mode.Flag;
                checkbox.OnToggled += _ =>
                {
                    if (_updating)
                        return;
                    OnSendMessage?.Invoke(new ShieldGeneratorToggleModeMessage { Mode = modeFlag });
                };

                _modeBoxes[modeFlag] = checkbox;
                _modesContainer.AddChild(checkbox);
            }
        }

        foreach (var mode in state.Modes)
        {
            if (!_modeBoxes.TryGetValue(mode.Flag, out var checkbox))
                continue;

            checkbox.Pressed = mode.Enabled;
            checkbox.Disabled = mode.HackedOnly && !mode.Hackable;
            checkbox.ToolTip = Loc.GetString(mode.Description);
        }

        _updating = false;
    }

    private void OnTogglePressed(BaseButton.ButtonEventArgs args)
    {
        OnSendMessage?.Invoke(new ShieldGeneratorToggleMessage());
    }

    private void OnEmergencyPressed(BaseButton.ButtonEventArgs args)
    {
        OnSendMessage?.Invoke(new ShieldGeneratorEmergencyShutdownMessage());
    }

    private void OnApplyInputCap(BaseButton.ButtonEventArgs args)
    {
        OnSendMessage?.Invoke(new ShieldGeneratorSetInputCapMessage { InputCap = _inputCap.Value * 1000f });
    }
}