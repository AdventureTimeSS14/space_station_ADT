using Content.Client.ADT.Medical.CrewMonitoring;
using Content.Client.PDA;
using Content.Shared.ADT.Medical.CrewMonitoring;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Medical.CrewMonitoring;

public sealed class ADTCrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ADTCrewMonitoringWindow? _menu;

    public ADTCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ADTCrewMonitoringWindow>();

        //ADT-Tweak Start - New Monitor: PdaBorderColor / UiVisuals theme
        if (EntMan.TryGetComponent<PdaBorderColorComponent>(Owner, out var border))
        {
            _menu.BorderColor = border.BorderColor;
        }

        if (EntMan.TryGetComponent<ADTCrewMonitoringUiVisualsComponent>(Owner, out var visuals))
            _menu.ApplyScreenTheme(visuals.ThemeColor);
        //ADT-Tweak End

        //ADT-Tweak Start - New Monitor: BUI callbacks
        _menu.OnAlertMutedChanged = muted => SendMessage(new ADTCrewMonitoringSetAlertMutedMessage(muted));
        _menu.OnAlertVolumeChanged = volume => SendMessage(new ADTCrewMonitoringSetAlertVolumeMessage(volume));
        _menu.OnSelectServer = server => SendMessage(new ADTCrewMonitoringSelectServerMessage(server));
        _menu.OnScanStarted = () => SendMessage(new ADTCrewMonitoringScanStartMessage());
        _menu.OnScanComplete = () => SendMessage(new ADTCrewMonitoringScanCompleteMessage());
        _menu.OnRescan = () => SendMessage(new ADTCrewMonitoringRescanMessage());
        _menu.OnResetSensors = () => SendMessage(new ADTCrewMonitoringResetSensorsMessage());
        _menu.OnAiEyeTeleport = target => SendMessage(new ADTCrewMonitoringAiEyeTeleportMessage(target));
        //ADT-Tweak End
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ADTCrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                //ADT-Tweak Start - New Monitor: pass full BUI state (was Sensors list + bool)
                _menu?.ShowSensors(st, Owner, xform?.Coordinates);
                //ADT-Tweak End
                break;
        }
    }
}
