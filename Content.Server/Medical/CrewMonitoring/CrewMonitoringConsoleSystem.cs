using System.Linq;
using System.Numerics;
using Content.Server.Medical.SuitSensors;
using Content.Server.ADT.Medical.CrewMonitoring;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Shared.PowerCell;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
// ADT-Tweak-Start
using Content.Shared.PowerCell.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
// ADT-Tweak-End

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    // ADT-Tweak Start - New Monitor: server subscriber
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly CrewMonitoringServerSystem _crewServers = default!;
    [Dependency] private readonly SuitSensorSystem _suitSensors = default!;
    // ADT-Tweak End


    // ADT-Tweak-Start
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    // ADT-Tweak-End

    // ADT-Tweak Start - New Monitor: scan/select/alerts/navmap/offline
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float ScanDuration = 5f;
    private const float ConsoleUpdateInterval = 0.5f;
    private const float CritAlertResyncDelay = 1.0f;
    private float _consoleUpdateAccumulator;
    private List<Entity<MapGridComponent>> _navMapGridBuffer = new();
    // ADT-Tweak End

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, GotEmaggedEvent>(OnEmagged); // ADT-Tweak
        // ADT-Tweak Start - New Monitor: BUI + server update subscriptions
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSetAlertMutedMessage>(OnSetAlertMuted);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSetAlertVolumeMessage>(OnSetAlertVolume);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringSelectServerMessage>(OnSelectServer);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringScanStartMessage>(OnScanStart);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringScanCompleteMessage>(OnScanComplete);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringRescanMessage>(OnRescan);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, CrewMonitoringResetSensorsMessage>(OnResetSensors);
        SubscribeLocalEvent<CrewMonitoringServerComponent, EntityTerminatingEvent>(OnServerTerminating);
        SubscribeLocalEvent<CrewMonitoringServerComponent, CrewMonitoringServerUpdateEvent>(OnServerUpdate);
        // ADT-Tweak End
    }

    // ADT-Tweak Start - New Monitor: scan / select / alert update loops
    private void OnScanStart(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringScanStartMessage args)
    {
        component.HasScanned = false;
        component.ScanStartedAt = _gameTiming.CurTime;
    }

    private void OnScanComplete(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringScanCompleteMessage args)
    {
        // Client already waited ScanDuration. If ScanStart was dropped, ScanStartedAt
        // is null — still complete so the UI cannot stick on "waiting forever".
        if (component.ScanStartedAt != null &&
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
        DiscoverServers(uid, component);
        PopulateNavMapsForConsole(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnRescan(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringRescanMessage args)
    {
        var now = _gameTiming.CurTime;
        DiscoverServers(uid, component);

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

            if (server.LastSensorSnapshot.Count == 0)
            {
                if (component.ConnectedSensors.Count != 0)
                    component.ConnectedSensors = new();
            }
            else
            {
                component.ConnectedSensors = CrewMonitoringServerSystem.CopyLastSnapshot(server);
            }
            component.LastReferenceFrame = server.ReferenceFrame;

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
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: reset snapshots + force re-ingest
    private void OnResetSensors(EntityUid uid, CrewMonitoringConsoleComponent component, CrewMonitoringResetSensorsMessage args)
    {
        if (!component.HasScanned)
            return;

        // Drop the console's retained view immediately so the UI goes empty until
        component.ConnectedSensors = new();

        // Always wipe the selected server even if unpowered / out of range, otherwise a
        // later reconnect would push the pre-reset last-known snapshot back into the UI.
        if (component.SelectedServerUid is { } selectedUid &&
            !Deleted(selectedUid) &&
            TryComp<CrewMonitoringServerComponent>(selectedUid, out var selectedServer))
        {
            selectedServer.SensorStatus.Clear();
            selectedServer.LastSensorSnapshot.Clear();
            selectedServer.SnapshotDirty = true;
        }
        else
        {
            var query = EntityQueryEnumerator<CrewMonitoringServerComponent>();
            while (query.MoveNext(out var serverUid, out var server))
            {
                if (!IsServerInRange(uid, serverUid))
                    continue;

                if (server.SubscriberConsoles.Count == 0 || !server.SubscriberConsoles.Contains(uid))
                {
                    var known = false;
                    foreach (var entry in component.CachedServers)
                    {
                        if (TryGetEntity(entry.NetEntity, out var knownUid) && knownUid == serverUid)
                        {
                            known = true;
                            break;
                        }
                    }

                    if (!known)
                        continue;
                }

                server.SensorStatus.Clear();
                server.LastSensorSnapshot.Clear();
                server.SnapshotDirty = true;
            }
        }

        component.KnownAlertStates.Clear();
        component.NextCritAlertTime = TimeSpan.Zero;

        var canRefill = component.SelectedServerUid is { } refillUid &&
                        !Deleted(refillUid) &&
                        IsServerInRange(uid, refillUid) &&
                        IsServerResponding(refillUid);

        if (canRefill)
        {
            _suitSensors.ForceImmediateReports();
            component.CritAlertResyncPending = true;
            component.CritAlertResyncReadyAt = _gameTiming.CurTime + TimeSpan.FromSeconds(CritAlertResyncDelay);
        }
        else
        {
            component.CritAlertResyncPending = false;
            component.CritAlertResyncReadyAt = TimeSpan.Zero;
            component.LastPacketTime = TimeSpan.Zero;
            component.OfflineStateSent = true;
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

        if (component.SelectedServerUid == serverUid.Value)
        {
            if (TryComp<CrewMonitoringServerComponent>(serverUid.Value, out var activeServer))
                _crewServers.RemoveSubscriber(activeServer, uid);

            component.SelectedServerUid = null;
            component.ConnectedSensors = new();
            component.LastReferenceFrame = null;
            component.LastServerUid = null;
            component.LastPacketTime = TimeSpan.Zero;
            component.OfflineStateSent = true;
            component.KnownAlertStates.Clear();
            component.NextCritAlertTime = TimeSpan.Zero;
            component.CritAlertResyncPending = false;
            component.ServersListDirty = true;
            PopulateNavMapsForConsole(uid, component);
            UpdateUserInterface(uid, component);
            return;
        }

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
        if (serverComp.LastSensorSnapshot.Count == 0)
        {
            if (component.ConnectedSensors.Count != 0)
                component.ConnectedSensors = new();
        }
        else
        {
            component.ConnectedSensors = CrewMonitoringServerSystem.CopyLastSnapshot(serverComp);
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
        // Fresh selection: empty KnownAlertStates → one beep if anyone is already crit/dead.
        component.KnownAlertStates.Clear();
        ProcessCritAlertSound(uid, component); //ADT-Tweak: NewMonitor
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
        UpdatePostResetCritAlerts();
        UpdateCritAlertReminders();
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
            comp.KnownAlertStates.Clear(); //ADT-Tweak: NewMonitor — re-alert after reconnect
            comp.NextCritAlertTime = TimeSpan.Zero;
            UpdateUserInterface(uid, comp);
        }
    }
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: edge + 30s reminder crit/dead alerts
    /// <summary>
    /// After Reset Sensors silence window: one beep if any crit/dead remain, then resume edges.
    /// </summary>
    private void UpdatePostResetCritAlerts()
    {
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.CritAlertResyncPending || now < comp.CritAlertResyncReadyAt)
                continue;

            comp.CritAlertResyncPending = false;

            if (!IsSelectedServerOnline(comp, now))
            {
                comp.KnownAlertStates.Clear();
                comp.NextCritAlertTime = TimeSpan.Zero;
                continue;
            }

            var current = CollectAlertStates(FilterSensors(comp, comp.ConnectedSensors.Values));
            comp.KnownAlertStates.Clear();
            foreach (var (owner, isDead) in current)
                comp.KnownAlertStates[owner] = isDead;

            if (current.Count == 0)
            {
                comp.NextCritAlertTime = TimeSpan.Zero;
                continue;
            }

            TryPlayCritAlertSound(uid, comp);
            comp.NextCritAlertTime = now + TimeSpan.FromSeconds(comp.CritAlertInterval);
        }
    }

    /// <summary>
    /// Reminder ping every <see cref="CrewMonitoringConsoleComponent.CritAlertInterval"/>
    /// while any filtered sensor remains crit or dead.
    /// </summary>
    private void UpdateCritAlertReminders()
    {
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CritAlertResyncPending)
                continue;

            if (!IsSelectedServerOnline(comp, now))
            {
                comp.NextCritAlertTime = TimeSpan.Zero;
                continue;
            }

            if (!HasAlertCondition(FilterSensors(comp, comp.ConnectedSensors.Values)))
            {
                comp.NextCritAlertTime = TimeSpan.Zero;
                continue;
            }

            // Edge path schedules the first reminder; wait until then.
            if (comp.NextCritAlertTime == TimeSpan.Zero || now < comp.NextCritAlertTime)
                continue;

            TryPlayCritAlertSound(uid, comp);
            comp.NextCritAlertTime = now + TimeSpan.FromSeconds(comp.CritAlertInterval);
        }
    }

    /// <summary>
    /// Immediate alert only on worsening edges: enter crit/dead, or crit -> dead.
    /// dead -> crit updates state silently and leaves the reminder timer alone;
    /// crit -> alive clears KnownAlertStates for that wearer and resets NextCritAlertTime when none remain.
    /// </summary>
    private void ProcessCritAlertSound(EntityUid uid, CrewMonitoringConsoleComponent comp)
    {
        // Reset Sensors: ignore edges until baseline beep in UpdatePostResetCritAlerts.
        if (comp.CritAlertResyncPending)
            return;

        var now = _gameTiming.CurTime;
        if (!IsSelectedServerOnline(comp, now))
        {
            comp.KnownAlertStates.Clear();
            comp.NextCritAlertTime = TimeSpan.Zero;
            return;
        }

        // Match UI filter (department handhelds) so alerts only cover visible crew.
        var current = CollectAlertStates(FilterSensors(comp, comp.ConnectedSensors.Values));

        var shouldPlay = false;
        foreach (var (owner, isDead) in current)
        {
            // New alert condition (alive/bad -> crit or dead).
            if (!comp.KnownAlertStates.TryGetValue(owner, out var wasDead))
            {
                shouldPlay = true;
                break;
            }

            // Worsening only: crit → dead. dead → crit is silent.
            if (!wasDead && isDead)
            {
                shouldPlay = true;
                break;
            }
        }

        comp.KnownAlertStates.Clear();
        foreach (var (owner, isDead) in current)
            comp.KnownAlertStates[owner] = isDead;

        if (current.Count == 0)
        {
            comp.NextCritAlertTime = TimeSpan.Zero;
            return;
        }

        if (!shouldPlay)
        {
            // Still alerting — ensure a reminder is scheduled if somehow unset.
            if (comp.NextCritAlertTime == TimeSpan.Zero)
                comp.NextCritAlertTime = now + TimeSpan.FromSeconds(comp.CritAlertInterval);
            return;
        }

        TryPlayCritAlertSound(uid, comp);
        // Always arm the reminder — mute only suppresses audio, not the schedule.
        comp.NextCritAlertTime = now + TimeSpan.FromSeconds(comp.CritAlertInterval);
    }

    private static Dictionary<NetEntity, bool> CollectAlertStates(IEnumerable<SuitSensorStatus> sensors)
    {
        var current = new Dictionary<NetEntity, bool>();
        foreach (var sensor in sensors)
        {
            if (!sensor.IsActive || sensor.Mode == SuitSensorMode.SensorOff)
                continue;

            if (!sensor.IsAlive)
                current[sensor.OwnerUid] = true;
            else if (sensor.IsCritical)
                current[sensor.OwnerUid] = false;
        }

        return current;
    }

    private bool TryPlayCritAlertSound(EntityUid uid, CrewMonitoringConsoleComponent comp)
    {
        //ADT-Tweak: Aghost UI - no beep from the ghost. Physical in-world monitors still PlayPvs.
        if (comp.SuppressCritAlertSound || comp.AlertMuted || comp.AlertVolume <= 0.01f)
            return false;

        // Handheld: no beep without a battery or with a depleted cell. Consoles skip (no PowerCellDraw).
        if (HasComp<PowerCellSlotComponent>(uid) && !_cell.HasActivatableCharge(uid))
            return false;

        var baseVolume = AudioParams.Default.Volume;
        if (comp.CritAlertSound.Params.Volume != 0)
            baseVolume = comp.CritAlertSound.Params.Volume;

        var volumeDb = MathHelper.Lerp(baseVolume - 32f, baseVolume, Math.Clamp(comp.AlertVolume, 0f, 1f));
        _audio.PlayPvs(comp.CritAlertSound, uid, AudioParams.Default.WithVolume(volumeDb));
        return true;
    }
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: OnRemove unsubscribes from server
    private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
    {
        if (component.SelectedServerUid != null && TryComp<CrewMonitoringServerComponent>(component.SelectedServerUid.Value, out var serverComp))
            _crewServers.RemoveSubscriber(serverComp, uid);
        component.ConnectedSensors = new();
        component.LastReferenceFrame = null;
    }
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: server update / alert BUI handlers
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

        ProcessCritAlertSound(consoleUid, console); //ADT-Tweak: NewMonitor — edge alert on snapshot
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
    // ADT-Tweak End

    private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        // ADT-Tweak Start - New Monitor: re-subscribe + populate navmaps on UI open
        // Re-attach to the selected server if the previous subscription was dropped
        // (server idle cleanup, restart, etc.) while the console kept SelectedServerUid.
        if (component.SelectedServerUid != null &&
            TryComp<CrewMonitoringServerComponent>(component.SelectedServerUid.Value, out var serverComp) &&
            IsServerInRange(uid, component.SelectedServerUid.Value))
        {
            _crewServers.AddSubscriber(serverComp, uid);
        }

        PopulateNavMapsForConsole(uid, component);
        // ADT-Tweak End
        UpdateUserInterface(uid, component);
    }

    // ADT-Tweak Start - New Monitor: extended UpdateUserInterface state
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
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: navmap / filter / server discovery helpers
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
            if (!sensor.IsActive ||
                sensor.Mode == SuitSensorMode.SensorOff)
            {
                continue;
            }

            // Alert only for dead or MobState.Critical (unconscious), not high damage while awake.
            if (!sensor.IsAlive || sensor.IsCritical) //ADT-Tweak: NewMonitor
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

    /// <summary>
    /// Full server discovery — only on initial scan / explicit rescan.
    /// Between scans the list is frozen; status of known entries may still refresh.
    /// </summary>
    private void DiscoverServers(EntityUid consoleUid, CrewMonitoringConsoleComponent component)
    {
        component.CachedServers = GetServersInRange(consoleUid);
        component.LastServersRefresh = _gameTiming.CurTime;
        component.ServersListDirty = false;
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

        RefreshCachedServerStatus(consoleUid, component);
        component.LastServersRefresh = now;
        component.ServersListDirty = false;
        return component.CachedServers;
    }

    /// <summary>
    /// Updates online/coords/range for servers already discovered by scan/rescan.
    /// Does not add newly appeared servers — that requires DiscoverServers.
    /// </summary>
    private void RefreshCachedServerStatus(EntityUid consoleUid, CrewMonitoringConsoleComponent component)
    {
        foreach (var entry in component.CachedServers)
        {
            if (!TryGetEntity(entry.NetEntity, out var serverUid) ||
                !TryComp<CrewMonitoringServerComponent>(serverUid.Value, out var serverComp) ||
                !TryComp(serverUid.Value, out TransformComponent? serverXform) ||
                !IsServerInRange(consoleUid, serverUid.Value))
            {
                entry.IsOnline = false;
                continue;
            }

            entry.Coordinates = GetNetCoordinates(serverXform.Coordinates);
            entry.ServerAddress = serverComp.ServerAddress ?? string.Empty;
            entry.IsOnline = IsServerResponding(serverUid.Value);
            entry.GridName = serverXform.GridUid != null ? Name(serverXform.GridUid.Value) : string.Empty;
            entry.SensorRange = TryComp<WirelessNetworkComponent>(serverUid.Value, out var wireless)
                ? wireless.Range
                : 0f;
        }
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
