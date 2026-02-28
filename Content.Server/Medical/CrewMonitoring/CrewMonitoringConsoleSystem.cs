using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.PowerCell;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
// ADT-Tweak-Start
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
// ADT-Tweak-End

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;


    // ADT-Tweak-Start
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationSystem _station = default!;
    // ADT-Tweak-End

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GotEmaggedEvent>(OnEmagged); // ADT-Tweak
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSetAlertMutedMessage>(OnSetAlertMuted);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSelectServerMessage>(OnSelectServer);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringScanCompleteMessage>(OnScanComplete);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringRescanMessage>(OnRescan);
    }

    private void OnScanComplete(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringScanCompleteMessage args)
    {
        component.HasScanned = true;
        UpdateUserInterface(uid, component);
    }

    private void OnRescan(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringRescanMessage args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnSelectServer(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringSelectServerMessage args)
    {
        if (!TryGetEntity(args.Server, out var serverUid))
            return;
        if (!HasComp<CrewMonitoringServerComponent>(serverUid.Value))
            return;

        if (component.SelectedServerUid != null &&
            TryComp<CrewMonitoringServerComponent>(component.SelectedServerUid.Value, out var prev))
        {
            prev.SubscriberConsoles.Remove(uid);
        }

        component.SelectedServerUid = serverUid;
        if (TryComp<CrewMonitoringServerComponent>(serverUid.Value, out var serverComp))
            serverComp.SubscriberConsoles.Add(uid);

        // Сбросить старое состояние, чтобы сразу перестать считать прошлый сервер активным
        // и дождаться первого пакета от нового сервера.
        component.ConnectedSensors.Clear();
        component.CachedSensors.Clear();
        component.LastServerName = string.Empty;
        component.LastServerAddress = string.Empty;
        component.LastGridName = string.Empty;
        component.LastGridUid = null;
        component.LastServerUid = null;
        component.LastOfflineStatePush = TimeSpan.Zero;
        // форсим "оффлайн" до получения данных от нового сервера
        component.LastPacketTime = _gameTiming.CurTime - TimeSpan.FromSeconds(component.SensorTimeout + 1);

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Throttle: push offline state at most once per second.
    /// </summary>
    private const float OfflineStatePushInterval = 1f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateCritAlerts();
        UpdateOfflineConsoles();
    }

    /// <summary>
    /// When connection is lost, push cached state to open UIs so client shows "Server Offline" and last snapshot.
    /// </summary>
    private void UpdateOfflineConsoles()
    {
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
                continue;

            var isOnline = (now - comp.LastPacketTime).TotalSeconds <= comp.SensorTimeout;
            if (isOnline || comp.CachedSensors.Count == 0)
                continue;

            if ((now - comp.LastOfflineStatePush).TotalSeconds < OfflineStatePushInterval)
                continue;

            comp.LastOfflineStatePush = now;
            UpdateUserInterface(uid, comp);
        }
    }

    /// <summary>
    /// When any connected sensor is crit or dead, play alert at the console and repeat every CritAlertInterval.
    /// </summary>
    private void UpdateCritAlerts()
    {
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var isOnline = (now - comp.LastPacketTime).TotalSeconds <= comp.SensorTimeout;
            if (!isOnline)
                continue; // Don't play alert from stale cached data

            var sensors = comp.ConnectedSensors.Values;
            var hasCritOrDead = sensors.Any(s => !s.IsAlive || (s.DamagePercentage != null && s.DamagePercentage.Value >= 0.8f));

            if (!hasCritOrDead)
            {
                comp.NextCritAlertTime = TimeSpan.Zero;
                continue;
            }

            if (comp.NextCritAlertTime != TimeSpan.Zero && now < comp.NextCritAlertTime)
                continue;

            if (comp.AlertMuted)
                continue;

            _audio.PlayPvs(comp.CritAlertSound, uid);
            comp.NextCritAlertTime = now + TimeSpan.FromSeconds(comp.CritAlertInterval);
        }
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        if (component.SelectedServerUid != null && TryComp<CrewMonitoringServerComponent>(component.SelectedServerUid.Value, out var serverComp))
            serverComp.SubscriberConsoles.Remove(uid);
        component.ConnectedSensors.Clear();
        component.CachedSensors.Clear();
    }

    private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
    {
        if (component.SelectedServerUid == null || args.Sender != component.SelectedServerUid.Value)
            return;

        var payload = args.Data;

        // Check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
            return;

        component.ConnectedSensors = sensorStatus;
        component.LastServerName = payload.TryGetValue(SuitSensorConstants.NET_SERVER_NAME, out var nameObj) && nameObj is string n ? n : string.Empty;
        component.LastServerAddress = payload.TryGetValue(SuitSensorConstants.NET_SERVER_ADDRESS, out var addrObj) && addrObj is string a ? a : string.Empty;
        component.LastGridName = payload.TryGetValue(SuitSensorConstants.NET_GRID_NAME, out var gridNameObj) && gridNameObj is string gn ? gn : string.Empty;
        component.LastGridUid = payload.TryGetValue(SuitSensorConstants.NET_GRID_UID, out var gridUidObj) && gridUidObj is NetEntity gnu ? gnu : null;
        component.LastPacketTime = _gameTiming.CurTime;
        component.LastServerUid = args.Sender;
        component.CachedSensors = new Dictionary<string, SuitSensorStatus>(sensorStatus);
        component.CachedServerName = component.LastServerName;
        component.CachedServerAddress = component.LastServerAddress;
        component.CachedGridName = component.LastGridName;
        component.CachedGridUid = component.LastGridUid;
        UpdateUserInterface(uid, component);
    }

    private void OnSetAlertMuted(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringSetAlertMutedMessage args)
    {
        component.AlertMuted = args.Muted;
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(uid);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        // When connection lost (timeout), show cached snapshot instead of empty
        var now = _gameTiming.CurTime;
        var isOnline = (now - component.LastPacketTime).TotalSeconds <= component.SensorTimeout;
        var sourceSensors = isOnline ? component.ConnectedSensors : component.CachedSensors;
        var serverName = isOnline ? component.LastServerName : component.CachedServerName;
        var serverAddress = isOnline ? component.LastServerAddress : component.CachedServerAddress;

        // Update all sensors info
        var allSensors = sourceSensors.Values.ToList();

        //ADT-Tweak-Start: Filtering by departments
        if (component.Departments.Count > 0)
        {
            var allowedDepartmentNames = new List<string>();
            foreach (var dept in component.Departments)
            {
                var deptId = dept.ToString();
                if (_proto.TryIndex<DepartmentPrototype>(deptId, out var department))
                {
                    var localizedDepartmentName = Loc.GetString(department.Name);
                    allowedDepartmentNames.Add(localizedDepartmentName);
                }
            }

            if (allowedDepartmentNames.Count > 0)
            {
                allSensors = allSensors.Where(s => s.JobDepartments != null &&
                    s.JobDepartments.Any(dept => allowedDepartmentNames.Contains(dept))).ToList();
            }
        }
        // ADT-Tweak-End

        var serverOnline = isOnline && allSensors.Count > 0;
        var alertActive = allSensors.Any(s => !s.IsAlive || (s.DamagePercentage != null && s.DamagePercentage.Value >= 0.8f));
        var stationCode = GetStationCode(uid);
        var servers = component.HasScanned ? GetServersInRange(uid, component, isOnline) : new List<CrewMonitoringServerEntry>();
        var gridName = isOnline ? component.LastGridName : component.CachedGridName;
        var serverGridUid = isOnline ? component.LastGridUid : component.CachedGridUid;

        var selectedServerNet = component.SelectedServerUid != null ? GetNetEntity(component.SelectedServerUid.Value) : (NetEntity?) null;
        _uiSystem.SetUiState(uid, CrewMonitoringUIKey.Key, new CrewMonitoringState(
            allSensors,
            component.IsEmagged,
            serverOnline,
            serverName,
            serverAddress,
            stationCode,
            alertActive,
            component.AlertMuted,
            servers,
            component.HasScanned,
            gridName,
            serverGridUid,
            selectedServerNet));
    }

    private List<CrewMonitoringServerEntry> GetServersInRange(EntityUid consoleUid, CrewMonitoringConsoleComponent component, bool connectionOnline)
    {
        var seen = new HashSet<NetEntity>();
        var list = new List<CrewMonitoringServerEntry>();
        var consoleXform = Transform(consoleUid);
        var consolePos = consoleXform.WorldPosition;
        var range = TryComp<WirelessNetworkComponent>(consoleUid, out var wireless) ? wireless.Range : 500;

        var query = EntityQueryEnumerator<CrewMonitoringServerComponent, TransformComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp, out var serverXform))
        {
            var netEntity = GetNetEntity(serverUid);
            if (!seen.Add(netEntity))
                continue;

            var dist = (serverXform.WorldPosition - consolePos).Length();
            if (dist > range)
                continue;

            var coords = GetNetCoordinates(serverXform.Coordinates);
            var address = serverComp.ServerAddress ?? string.Empty;
            var isOnline = connectionOnline && component.LastServerUid == serverUid;
            var gridName = serverXform.GridUid != null ? Name(serverXform.GridUid.Value) : string.Empty;

            list.Add(new CrewMonitoringServerEntry(netEntity, coords, address, isOnline, gridName));
        }

        return list;
    }

    private string GetStationCode(EntityUid uid)
    {
        var xform = Transform(uid);
        var station = xform.GridUid != null ? _station.GetOwningStation(xform.GridUid.Value) : null;
        if (station == null)
            return string.Empty;
        var hash = (uint) station.Value.GetHashCode();
        return $"ST-{(hash % 10000):D4}";
    }

    // ADT-Tweak-Start
    private void OnEmagged(EntityUid uid, CrewMonitoringConsoleComponent component, ref GotEmaggedEvent ev)
    {
        if (ev.Handled || component.IsEmagged)
            return;

        _audio.PlayPvs(component.SparkSound, uid);
        _popup.PopupEntity(Loc.GetString("crew-monitoring-component-upgrade-emag"), uid);

        component.IsEmagged = true;
        UpdateUserInterface(uid, component);
        ev.Handled = true;
    }
    // ADT-Tweak-End
}
