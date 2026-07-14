using Content.Client.PDA;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;

namespace Content.Client.Medical.CrewMonitoring;

public sealed class CrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CrewMonitoringWindow? _menu;

    public CrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        EntityUid? gridUid = null;
        var stationName = string.Empty;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;

            if (gridUid != null &&
                EntMan.TryGetComponent<MetaDataComponent>(gridUid.Value, out var metaData))
            {
                stationName = metaData.EntityName;
            }
        }

        _menu = this.CreateWindow<CrewMonitoringWindow>();

        // Same pipeline as PDA: bezel/accents come from PdaBorderColor on this device.
        if (EntMan.TryGetComponent<PdaBorderColorComponent>(Owner, out var border))
        {
            _menu.BorderColor = border.BorderColor;
            _menu.AccentHColor = border.AccentHColor;
            _menu.AccentVColor = border.AccentVColor;
        }

        if (EntMan.TryGetComponent<CrewMonitoringUiVisualsComponent>(Owner, out var visuals))
            _menu.ApplyScreenTheme(visuals.ThemeColor);

        _menu.Set(stationName, gridUid);
        _menu.OnAlertMutedChanged = muted => SendMessage(new CrewMonitoringSetAlertMutedMessage(muted));
        _menu.OnAlertVolumeChanged = volume => SendMessage(new CrewMonitoringSetAlertVolumeMessage(volume));
        _menu.OnSelectServer = server => SendMessage(new CrewMonitoringSelectServerMessage(server));
        _menu.OnScanStarted = () => SendMessage(new CrewMonitoringScanStartMessage());
        _menu.OnScanComplete = () => SendMessage(new CrewMonitoringScanCompleteMessage());
        _menu.OnRescan = () => SendMessage(new CrewMonitoringRescanMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                _menu?.ShowSensors(st, Owner, xform?.Coordinates);
                break;
        }
    }
}
