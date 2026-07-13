using System.Linq;
using System.Numerics;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.PowerCell;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly CrewMonitoringServerSystem _crewServers = default!;


    // ADT-Tweak-Start
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    // ADT-Tweak-End

    private const float ScanDuration = 5f;
    private const float ConsoleUpdateInterval = 0.5f;
    private float _consoleUpdateAccumulator;
    private List<Entity<MapGridComponent>> _navMapGridBuffer = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GotEmaggedEvent>(OnEmagged); // ADT-Tweak
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSetAlertMutedMessage>(OnSetAlertMuted);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSetAlertVolumeMessage>(OnSetAlertVolume);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSelectServerMessage>(OnSelectServer);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringScanStartMessage>(OnScanStart);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringScanCompleteMessage>(OnScanComplete);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringRescanMessage>(OnRescan);
        SubscribeLocalEvent<CrewMonitoringServerComponent, EntityTerminatingEvent>(OnServerTerminating);
        SubscribeLocalEvent<CrewMonitoringServerComponent, CrewMonitoringServerUpdateEvent>(OnServerUpdate);
    }

    private void OnScanStart(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringScanStartMessage args)
    {
        component.HasScanned = false;
        component.ScanStartedAt = _gameTiming.CurTime;
    }

    private void OnScanComplete(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringScanCompleteMessage args)
    {
        if (component.ScanStartedAt == null ||
            (_gameTiming.CurTime - component.ScanStartedAt.Value).TotalSeconds < ScanDuration)
        {
            return;
        }

        CompleteScan(uid, component);
    }

    private void CompleteScan(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        component.HasScanned = true;
        component.ScanStartedAt = null;
        component.ServersListDirty = true;
        PopulateNavMapsForConsole(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnRescan(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringRescanMessage args)
    {
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            if (!IsServerInRange(uid, serverUid))
                continue;

            // Every reachable server must send a complete snapshot after a rescan,
            // even if none of its sensor values changed.
            server.SnapshotDirty = true;

            if (component.SelectedServerUid != serverUid)
                continue;

            if (server.SensorStatus.Count == 0)
            {
                if (component.ConnectedSensors.Count != 0)
                    component.ConnectedSensors = new();
            }
            else
            {
                component.ConnectedSensors =
                    new Dictionary<string, SuitSensorStatus>(server.SensorStatus);
            }
            component.LastReferenceFrame = server.ReferenceFrame;
            component.ServersListDirty = true;

            if (IsServerResponding(serverUid))
            {
                component.LastPacketTime = now;
                component.LastServerUid = serverUid;
                component.OfflineStateSent = false;
            }
            else
            {
                component.OfflineStateSent = true;
            }
        }

        PopulateNavMapsForConsole(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnSelectServer(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringSelectServerMessage args)
    {
        if (!component.HasScanned)
            return;

        if (!TryGetEntity(args.Server, out var serverUid))
            return;
        if (!TryComp<CrewMonitoringServerComponent>(serverUid.Value, out var serverComp) ||
            !IsServerInRange(uid, serverUid.Value))
        {
            return;
        }

        if (component.SelectedServerUid != null &&
            TryComp<CrewMonitoringServerComponent>(component.SelectedServerUid.Value, out var prev))
        {
            _crewServers.RemoveSubscriber(prev, uid);
        }

        component.SelectedServerUid = serverUid;
        _crewServers.AddSubscriber(serverComp, uid);
        component.LastServerName = serverComp.ServerName ?? Name(serverUid.Value);
        component.LastServerAddress = serverComp.ServerAddress ?? string.Empty;
        var serverXform = Transform(serverUid.Value);
        component.LastGridName = serverXform.GridUid != null ? Name(serverXform.GridUid.Value) : string.Empty;
        component.LastGridUid = serverXform.GridUid != null ? GetNetEntity(serverXform.GridUid.Value) : null;
        component.LastServerUid = serverUid.Value;
        if (serverComp.SensorStatus.Count == 0)
        {
            if (component.ConnectedSensors.Count != 0)
                component.ConnectedSensors = new();
        }
        else
        {
            component.ConnectedSensors =
                new Dictionary<string, SuitSensorStatus>(serverComp.SensorStatus);
        }
        component.LastReferenceFrame = serverComp.ReferenceFrame;
        component.ServersListDirty = true;

        if (IsServerResponding(serverUid.Value))
        {
            // Pull a snapshot immediately, so sensors appear right after selecting a server.
            component.LastPacketTime = _gameTiming.CurTime;
            component.OfflineStateSent = false;
        }
        else
        {
            component.OfflineStateSent = true;
            component.LastPacketTime = _gameTiming.CurTime - TimeSpan.FromSeconds(component.SensorTimeout + 1);
        }

        PopulateNavMapsForConsole(uid, component);
        UpdateUserInterface(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _consoleUpdateAccumulator += frameTime;
        if (_consoleUpdateAccumulator < ConsoleUpdateInterval)
            return;

        _consoleUpdateAccumulator -= ConsoleUpdateInterval;
        UpdatePendingScans();
        UpdateCritAlerts();
        UpdateOfflineConsoles();
    }

    private void UpdatePendingScans()
    {
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScanStartedAt == null ||
                (now - component.ScanStartedAt.Value).TotalSeconds < ScanDuration)
            {
                continue;
            }

            CompleteScan(uid, component);
        }
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

            var isOnline = IsSelectedServerOnline(comp, now);
            if (isOnline || comp.SelectedServerUid == null || comp.OfflineStateSent)
                continue;

            comp.OfflineStateSent = true;
            comp.ServersListDirty = true;
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
            var isOnline = IsSelectedServerOnline(comp, now);
            if (!isOnline)
                continue; // Don't play alert from stale cached data

            var hasCritOrDead = HasAlertCondition(comp.ConnectedSensors.Values);

            if (!hasCritOrDead)
            {
                comp.NextCritAlertTime = TimeSpan.Zero;
                continue;
            }

            if (comp.NextCritAlertTime != TimeSpan.Zero && now < comp.NextCritAlertTime)
                continue;

            if (comp.AlertMuted)
                continue;

            if (comp.AlertVolume <= 0.01f)
                continue;

            var baseVolume = AudioParams.Default.Volume;
            if (comp.CritAlertSound.Params.Volume != 0)
                baseVolume = comp.CritAlertSound.Params.Volume;

            // Map 0..1 UI volume onto a usable dB range ending at the sound's configured volume.
            var volumeDb = MathHelper.Lerp(baseVolume - 32f, baseVolume, Math.Clamp(comp.AlertVolume, 0f, 1f));
            _audio.PlayPvs(comp.CritAlertSound, uid, AudioParams.Default.WithVolume(volumeDb));
            comp.NextCritAlertTime = now + TimeSpan.FromSeconds(comp.CritAlertInterval);
        }
    }

    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        if (component.SelectedServerUid != null && TryComp<CrewMonitoringServerComponent>(component.SelectedServerUid.Value, out var serverComp))
            _crewServers.RemoveSubscriber(serverComp, uid);
        component.ConnectedSensors = new();
        component.LastReferenceFrame = null;
    }

    private void OnServerTerminating(EntityUid uid, CrewMonitoringServerComponent component, ref EntityTerminatingEvent args)
    {
        var subscribers = component.SubscriberConsoles.ToArray();
        _crewServers.ClearSubscribers(component);

        foreach (var consoleUid in subscribers)
        {
            if (!TryComp<CrewMonitoringConsoleComponent>(consoleUid, out var console))
                continue;

            console.SelectedServerUid = null;
            console.LastServerUid = null;
            console.LastReferenceFrame = null;
            console.OfflineStateSent = true;
            UpdateUserInterface(consoleUid, console);
        }
    }

    private void OnServerUpdate(
        Entity<CrewMonitoringServerComponent> server,
        ref CrewMonitoringServerUpdateEvent args)
    {
        List<EntityUid>? staleSubscribers = null;
        foreach (var consoleUid in server.Comp.SubscriberConsoles)
        {
            if (Deleted(consoleUid) ||
                !HasComp<CrewMonitoringConsoleComponent>(consoleUid))
            {
                (staleSubscribers ??= new()).Add(consoleUid);
                continue;
            }

            args.Delivered |= ReceiveServerUpdate(
                consoleUid,
                server.Owner,
                server.Comp,
                args.Snapshot);
        }

        if (staleSubscribers == null)
            return;

        foreach (var stale in staleSubscribers)
            _crewServers.RemoveSubscriber(server.Comp, stale);
    }

    public bool ReceiveServerUpdate(
        EntityUid consoleUid,
        EntityUid serverUid,
        CrewMonitoringServerComponent server,
        Dictionary<string, SuitSensorStatus>? snapshot)
    {
        if (!TryComp<CrewMonitoringConsoleComponent>(consoleUid, out var console) ||
            console.SelectedServerUid != serverUid ||
            !IsServerInRange(consoleUid, serverUid))
        {
            return false;
        }

        var now = _gameTiming.CurTime;
        var wasOnline = IsSelectedServerOnline(console, now);
        console.LastPacketTime = now;
        console.LastServerUid = serverUid;
        console.OfflineStateSent = false;
        console.LastReferenceFrame = server.ReferenceFrame;

        if (snapshot == null)
        {
            if (!wasOnline)
                UpdateUserInterface(consoleUid, console);
            return true;
        }

        // Do not take ownership of the shared empty snapshot singleton.
        if (snapshot.Count == 0)
        {
            if (console.ConnectedSensors.Count != 0)
                console.ConnectedSensors = new();
        }
        else
        {
            console.ConnectedSensors = snapshot;
        }
        console.LastServerName = server.ServerName ?? Name(serverUid);
        console.LastServerAddress = server.ServerAddress ?? string.Empty;

        var serverXform = Transform(serverUid);
        console.LastGridName =
            serverXform.GridUid != null ? Name(serverXform.GridUid.Value) : string.Empty;
        console.LastGridUid =
            serverXform.GridUid != null ? GetNetEntity(serverXform.GridUid.Value) : null;

        UpdateUserInterface(consoleUid, console);
        return true;
    }

    private void OnSetAlertMuted(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringSetAlertMutedMessage args)
    {
        component.AlertMuted = args.Muted;
        UpdateUserInterface(uid, component);
    }

    private void OnSetAlertVolume(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringSetAlertVolumeMessage args)
    {
        component.AlertVolume = Math.Clamp(args.Volume, 0f, 1f);
        UpdateUserInterface(uid, component);
    }

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        PopulateNavMapsForConsole(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_uiSystem.IsUiOpen(uid, CrewMonitoringUIKey.Key))
            return;

        // Keep showing the last immutable snapshot when the connection times out.
        var now = _gameTiming.CurTime;
        var isOnline = IsSelectedServerOnline(component, now);
        var sourceSensors = component.ConnectedSensors;
        var serverName = component.LastServerName;
        var serverAddress = component.LastServerAddress;

        var allSensors = FilterSensors(component, sourceSensors.Values);

        var serverOnline = isOnline;
        var alertActive = HasAlertCondition(allSensors);
        var stationCode = GetStationCode(uid);
        List<CrewMonitoringServerEntry> servers;
        if (component.HasScanned)
            servers = GetCachedServersInRange(uid, component);
        else
        {
            if (component.CachedServers.Count > 0)
                component.CachedServers = new List<CrewMonitoringServerEntry>();
            servers = component.CachedServers;
        }
        var gridName = component.LastGridName;
        var serverGridUid = component.LastGridUid;

        var selectedServerNet = component.SelectedServerUid != null
            ? GetNetEntity(component.SelectedServerUid.Value)
            : (NetEntity?)null;
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
            selectedServerNet,
            component.LastReferenceFrame,
            component.AlertVolume));
    }

    private void PopulateNavMapsForConsole(EntityUid uid, CrewMonitoringConsoleComponent component)
    {
        var xform = Transform(uid);
        if (xform.GridUid != null)
            TryPopulateNavMap(component, xform.GridUid.Value);

        if (component.SelectedServerUid != null &&
            TryComp(component.SelectedServerUid.Value, out TransformComponent? serverXform) &&
            TryComp<WirelessNetworkComponent>(component.SelectedServerUid.Value, out var wireless))
        {
            EnsureNavMapsAround(component, serverXform, wireless.Range);
            return;
        }

        if (component.LastReferenceFrame != null &&
            TryGetEntity(component.LastReferenceFrame.FrameEntity, out var frameUid) &&
            HasComp<MapGridComponent>(frameUid.Value))
        {
            TryPopulateNavMap(component, frameUid.Value);
        }
    }

    private void EnsureNavMapsAround(CrewMonitoringConsoleComponent component, TransformComponent serverXform, float range)
    {
        if (range <= 0f)
            return;

        var center = _transform.GetWorldPosition(serverXform);
        var extent = new Vector2(range, range);
        _navMapGridBuffer.Clear();
        _mapManager.FindGridsIntersecting(
            serverXform.MapID,
            new Box2(center - extent, center + extent),
            ref _navMapGridBuffer,
            approx: true,
            includeMap: false);

        foreach (var grid in _navMapGridBuffer)
            TryPopulateNavMap(component, grid.Owner, grid.Comp);
    }

    private void TryPopulateNavMap(
        CrewMonitoringConsoleComponent component,
        EntityUid gridUid,
        MapGridComponent? mapGrid = null)
    {
        if (!component.PopulatedNavMapGrids.Add(gridUid))
            return;

        _navMap.EnsureNavMap(gridUid, mapGrid);
    }

    private static bool HasAlertCondition(IEnumerable<SuitSensorStatus> sensors)
    {
        foreach (var sensor in sensors)
        {
            if (!sensor.IsAlive ||
                (sensor.DamagePercentage != null && sensor.DamagePercentage.Value >= 0.8f))
            {
                return true;
            }
        }

        return false;
    }

    private List<SuitSensorStatus> FilterSensors(
        CrewMonitoringConsoleComponent component,
        IEnumerable<SuitSensorStatus> values)
    {
        EnsureDepartmentCache(component);

        if (component.IsEmagged || component.CachedDepartmentNames.Count == 0)
            return values is List<SuitSensorStatus> list ? list : values.ToList();

        var filtered = new List<SuitSensorStatus>();
        foreach (var sensor in values)
        {
            if (sensor.JobDepartments.Count == 0)
            {
                filtered.Add(sensor);
                continue;
            }

            foreach (var dept in sensor.JobDepartments)
            {
                if (component.CachedDepartmentNames.Contains(dept))
                {
                    filtered.Add(sensor);
                    break;
                }
            }
        }

        return filtered;
    }

    private void EnsureDepartmentCache(CrewMonitoringConsoleComponent component)
    {
        if (component.CachedDepartmentNames.Count > 0 || component.Departments.Count == 0)
            return;

        foreach (var dept in component.Departments)
        {
            if (_proto.TryIndex<DepartmentPrototype>(dept.ToString(), out var department))
                component.CachedDepartmentNames.Add(Loc.GetString(department.Name));
        }
    }

    private List<CrewMonitoringServerEntry> GetCachedServersInRange(
        EntityUid consoleUid,
        CrewMonitoringConsoleComponent component)
    {
        var now = _gameTiming.CurTime;
        if (!component.ServersListDirty &&
            component.LastServersRefresh != TimeSpan.Zero &&
            (now - component.LastServersRefresh).TotalSeconds < ConsoleUpdateInterval)
        {
            return component.CachedServers;
        }

        component.CachedServers = GetServersInRange(consoleUid);
        component.LastServersRefresh = now;
        component.ServersListDirty = false;
        return component.CachedServers;
    }

    private List<CrewMonitoringServerEntry> GetServersInRange(EntityUid consoleUid)
    {
        var seen = new HashSet<NetEntity>();
        var list = new List<CrewMonitoringServerEntry>();

        var query = EntityQueryEnumerator<CrewMonitoringServerComponent, TransformComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp, out var serverXform))
        {
            var netEntity = GetNetEntity(serverUid);
            if (!seen.Add(netEntity))
                continue;

            if (!IsServerInRange(consoleUid, serverUid))
                continue;

            var coords = GetNetCoordinates(serverXform.Coordinates);
            var address = serverComp.ServerAddress ?? string.Empty;
            var isOnline = IsServerResponding(serverUid);
            var gridName = serverXform.GridUid != null ? Name(serverXform.GridUid.Value) : string.Empty;
            var sensorRange = TryComp<WirelessNetworkComponent>(serverUid, out var wireless)
                ? wireless.Range
                : 0f;

            list.Add(new CrewMonitoringServerEntry(netEntity, coords, address, isOnline, sensorRange, gridName));
        }

        return list;
    }

    private bool IsServerResponding(EntityUid serverUid)
    {
        if (!TryComp<ApcPowerReceiverComponent>(serverUid, out var power) || !power.Powered)
            return false;

        if (!TryComp<DeviceNetworkComponent>(serverUid, out var device) ||
            !_deviceNetwork.IsDeviceConnected(serverUid, device))
        {
            return false;
        }

        return true;
    }

    private bool IsServerInRange(EntityUid consoleUid, EntityUid serverUid)
    {
        if (Deleted(consoleUid) || Deleted(serverUid))
            return false;

        var consoleXform = Transform(consoleUid);
        var serverXform = Transform(serverUid);
        if (consoleXform.MapID != serverXform.MapID)
            return false;

        // Wireless packet range is determined by the sender. Crew monitor
        // updates are sent by the monitoring server.
        if (TryComp<WirelessNetworkComponent>(serverUid, out var wireless) &&
            (serverXform.WorldPosition - consoleXform.WorldPosition).Length() > wireless.Range)
        {
            return false;
        }

        return true;
    }

    private static bool IsSelectedServerOnline(CrewMonitoringConsoleComponent component, TimeSpan now)
    {
        return component.SelectedServerUid != null &&
               component.LastServerUid == component.SelectedServerUid &&
               (now - component.LastPacketTime).TotalSeconds <= component.SensorTimeout;
    }

    private string GetStationCode(EntityUid uid)
    {
        var xform = Transform(uid);
        var station = xform.GridUid != null ? _station.GetOwningStation(xform.GridUid.Value) : null;
        if (station == null)
            return string.Empty;
        var hash = (uint)station.Value.GetHashCode();
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
