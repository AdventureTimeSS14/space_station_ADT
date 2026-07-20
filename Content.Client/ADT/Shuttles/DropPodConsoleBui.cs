using System.Collections.Generic;
using System.Numerics;
using Content.Client.Administration.UI.CustomControls;
using Content.Shared.ADT.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Shuttles;

[UsedImplicitly]
public sealed class DropPodConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private DefaultWindow? _window;
    private Label? _statusLabel;
    private Label? _selectedLabel;
    private Button? _deployButton;
    private BoxContainer? _beaconList;
    private DropPodNavMapControl? _mapControl;

    private Label? _tcLabel;
    private Label? _warStatusLabel;

    private NetEntity? _selectedBeacon;
    private string? _selectedBeaconName;
    private List<DropPodBeaconInfo> _beacons = new();
    private bool _canLaunch;
    private bool _alreadyLaunched;
    private int _tcBalance;
    private int _tcCost;

    protected override void Open()
    {
        base.Open();
        TryInitWindow();
        UpdateState(State);
    }

    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state is not DropPodConsoleBuiState s)
            return;

        TryInitWindow();

        _beacons = s.ValidBeacons;
        _canLaunch = s.CanLaunch;
        _alreadyLaunched = s.AlreadyLaunched;
        _tcBalance = s.TcBalance;
        _tcCost = s.CurrentCost;

        if (_warStatusLabel != null)
        {
            if (s.IsAtWar)
            {
                _warStatusLabel.Text = Loc.GetString("drop-pod-console-war-active", ("time", s.WarCooldownRemaining));
                _warStatusLabel.FontColorOverride = Color.OrangeRed;
            }
            else
            {
                _warStatusLabel.Text = string.Empty;
            }
        }

        if (_mapControl != null && s.StationGrid.HasValue)
        {
            var stationUid = IoCManager.Resolve<IEntityManager>().GetEntity(s.StationGrid.Value);
            if (_mapControl.MapUid != stationUid)
            {
                _mapControl.MapUid = stationUid;
                _mapControl.ForceNavMapUpdate();
                _mapControl.CenterOnWorldPos(stationUid, s.StationWorldCenter);
            }
        }

        if (_selectedBeacon.HasValue && !_beacons.Exists(b => b.Uid == _selectedBeacon.Value))
        {
            _selectedBeacon = null;
            _selectedBeaconName = null;
            _mapControl?.SetSelectedBeacon(null);
        }

        RebuildBeaconList();
        RefreshState();

        if (!_window!.IsOpen)
            _window.OpenCentered();
    }

    private void TryInitWindow()
    {
        if (_window != null)
            return;

        _window = new DefaultWindow
        {
            Title = Loc.GetString("drop-pod-console-title"),
            MinSize = new Vector2(680, 420),
            Resizable = false,
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(8),
        };

        root.AddChild(BuildLeftPanel());
        root.AddChild(BuildRightPanel());

        _window.Contents.AddChild(root);
        _window.OnClose += Close;
    }

    private Control BuildLeftPanel()
    {
        var panel = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinSize = new Vector2(220, 0),
            Margin = new Thickness(0, 0, 10, 0),
        };

        _statusLabel = new Label
        {
            Margin = new Thickness(0, 0, 0, 4),
        };
        panel.AddChild(_statusLabel);

        _tcLabel = new Label
        {
            Margin = new Thickness(0, 0, 0, 8),
        };
        panel.AddChild(_tcLabel);

        _warStatusLabel = new Label
        {
            Margin = new Thickness(0, 0, 0, 8),
        };
        panel.AddChild(_warStatusLabel);

        panel.AddChild(new Label
        {
            Text = Loc.GetString("drop-pod-console-beacon-list-header"),
            FontColorOverride = Color.FromHex("#aaaaaa"),
            Margin = new Thickness(0, 0, 0, 4),
        });

        var scrollBox = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 8),
        };

        _beaconList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        scrollBox.AddChild(_beaconList);
        panel.AddChild(scrollBox);

        panel.AddChild(new HSeparator { Margin = new Thickness(0, 0, 0, 6) });

        var selectedHeader = new Label
        {
            Text = Loc.GetString("drop-pod-console-selected-header"),
            FontColorOverride = Color.FromHex("#aaaaaa"),
            Margin = new Thickness(0, 0, 0, 2),
        };
        panel.AddChild(selectedHeader);

        _selectedLabel = new Label
        {
            Margin = new Thickness(0, 0, 0, 8),
        };
        panel.AddChild(_selectedLabel);

        panel.AddChild(new HSeparator { Margin = new Thickness(0, 0, 0, 6) });

        panel.AddChild(new Label
        {
            Text = Loc.GetString("drop-pod-console-notice"),
            FontColorOverride = Color.FromHex("#ddaa00"),
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 8),
        });

        _deployButton = new Button
        {
            Text = Loc.GetString("drop-pod-console-deploy-button"),
            Disabled = true,
            HorizontalExpand = true,
            ModulateSelfOverride = Color.Red,
        };
        _deployButton.OnPressed += _ => OnDeployPressed();
        panel.AddChild(_deployButton);

        return panel;
    }

    private Control BuildRightPanel()
    {
        _mapControl = new DropPodNavMapControl
        {
            SetWidth = 420,
            SetHeight = 370,
            RectClipContent = true,
        };
        return _mapControl;
    }

    private void RebuildBeaconList()
    {
        if (_beaconList == null)
            return;

        _beaconList.RemoveAllChildren();

        foreach (var beacon in _beacons)
        {
            var b = beacon;
            var btn = new Button
            {
                Text = b.Name,
                HorizontalExpand = true,
                Margin = new Thickness(0, 0, 0, 2),
                Disabled = !_canLaunch || _alreadyLaunched,
                ModulateSelfOverride = _selectedBeacon == b.Uid ? Color.OrangeRed : null,
            };
            btn.OnPressed += _ => SelectBeacon(b.Uid, b.Name, b.WorldPos);
            _beaconList.AddChild(btn);
        }
    }

    private void SelectBeacon(NetEntity uid, string name, Vector2 worldPos)
    {
        if (!_canLaunch || _alreadyLaunched)
            return;

        _selectedBeacon = uid;
        _selectedBeaconName = name;
        _mapControl?.SetSelectedBeacon(worldPos);
        RefreshState();
    }

    private void RefreshState()
    {
        if (_statusLabel != null)
        {
            if (_alreadyLaunched)
            {
                _statusLabel.Text = Loc.GetString("drop-pod-console-status-launched");
                _statusLabel.FontColorOverride = Color.Gray;
            }
            else if (!_canLaunch)
            {
                _statusLabel.Text = Loc.GetString("drop-pod-console-status-not-ready");
                _statusLabel.FontColorOverride = Color.Yellow;
            }
            else
            {
                _statusLabel.Text = Loc.GetString("drop-pod-console-status-ready");
                _statusLabel.FontColorOverride = Color.LimeGreen;
            }
        }

        if (_selectedLabel != null)
        {
            _selectedLabel.Text = _selectedBeaconName != null
                ? _selectedBeaconName
                : Loc.GetString("drop-pod-console-sector-none");
            _selectedLabel.FontColorOverride = _selectedBeaconName != null ? Color.OrangeRed : Color.Gray;
        }

        if (_tcLabel != null)
        {
            _tcLabel.Text = Loc.GetString("drop-pod-console-tc-label", ("balance", _tcBalance), ("cost", _tcCost));
            _tcLabel.FontColorOverride = _tcBalance >= _tcCost ? Color.LimeGreen : Color.Red;
        }

        if (_deployButton != null)
            _deployButton.Disabled = _selectedBeacon == null || !_canLaunch || _alreadyLaunched;

        RebuildBeaconList();
    }

    private void OnDeployPressed()
    {
        if (_selectedBeacon == null)
            return;
        SendMessage(new DropPodConsoleDeployMessage { TargetBeacon = _selectedBeacon.Value });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Close();
    }
}
