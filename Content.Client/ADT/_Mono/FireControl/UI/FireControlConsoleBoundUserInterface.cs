using Content.Shared.ADT._Mono.FireControl;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client.ADT._Mono.FireControl.UI;

[UsedImplicitly]
public sealed class FireControlConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FireControlWindow? _window;

    public FireControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<FireControlWindow>();

        _window.OnServerRefresh += OnRefreshServer;

        _window.Radar.OnRadarClick += (coords) =>
        {
            var netCoords = EntMan.GetNetCoordinates(coords);
            SendFireMessage(netCoords);
        };

        _window.Radar.DefaultCursorShape = Control.CursorShape.Crosshair;
    
        // Add event handler for when weapons are selected/deselected
        _window.OnWeaponSelectionChanged += UpdateSelectedWeapons;
    }

    private void OnRefreshServer()
    {
        SendMessage(new FireControlConsoleRefreshServerMessage());
    }

    private void UpdateSelectedWeapons()
    {
        if (_window?.Radar is not FireControlNavControl navControl)
            return;

        var selectedWeapons = new HashSet<NetEntity>();
        foreach (var (netEntity, button) in _window.WeaponsList)
        {
            if (button.Pressed)
                selectedWeapons.Add(netEntity);
        }

        navControl.UpdateSelectedWeapons(selectedWeapons);
    }

    private void SendFireMessage(NetCoordinates coordinates)
    {
        if (_window == null)
            return;

        var selected = new List<NetEntity>();
        foreach (var button in _window.WeaponsList)
        {
            if (button.Value.Pressed)
                selected.Add(button.Key);
        }

        if (selected.Count > 0)
            SendMessage(new FireControlConsoleFireMessage(selected, coordinates));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not FireControlConsoleBoundInterfaceState castState)
            return;

        _window?.UpdateStatus(castState);
        if (_window?.Radar is FireControlNavControl navControl)
        {
            navControl.SetConsole(Owner);
            navControl.UpdateControllables(Owner, castState.FireControllables);

            // Update selected weapons when state updates
            UpdateSelectedWeapons();
        }
    }
}
