using System.Numerics;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float PublishInterval = 0.5f;
    private float _publishAccumulator;
    private readonly List<string> _removeBuffer = new();

    /// <summary>
    /// Number of monitoring servers that currently have at least one console subscriber.
    /// Suit sensors skip all reporting while this is zero.
    /// </summary>
    private int _serversWithSubscribers;

    /// <summary>True when any crew-monitor console is listening to any server.</summary>
    public bool HasAnySubscribers => _serversWithSubscribers > 0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SuitSensorComponent, SuitSensorReportEvent>(OnSensorReport);
    }

    /// <summary>
    /// Registers a console as listening to this server. Enables global sensor reporting on first subscriber.
    /// </summary>
    public void AddSubscriber(CrewMonitoringServerComponent server, EntityUid consoleUid)
    {
        if (!server.SubscriberConsoles.Add(consoleUid))
            return;

        if (server.SubscriberConsoles.Count == 1)
            _serversWithSubscribers++;

        server.SnapshotDirty = true;
    }

    /// <summary>
    /// Ingest a single suit-sensor report into every subscribed, powered server in range.
    /// Called directly from <see cref="SuitSensorSystem"/>.
    /// </summary>
    public void IngestReport(in SuitSensorReportEvent report)
    {
        // No console is listening anywhere — SuitSensorSystem should already skip, but
        // keep this guard so we never walk every monitoring server for nothing.
        if (!HasAnySubscribers)
            return;

        var key = $"sensor-{report.Status.SuitSensorUid}";
        var servers = EntityQueryEnumerator<
            CrewMonitoringServerComponent,
            WirelessNetworkComponent,
            ApcPowerReceiverComponent,
            TransformComponent>();

        while (servers.MoveNext(
                   out var serverUid,
                   out var server,
                   out var wireless,
                   out var power,
                   out var serverXform))
        {
            // Idle servers must not ingest sensor traffic at all.
            if (server.SubscriberConsoles.Count == 0)
                continue;

            if (!power.Powered)
                continue;

            var serverPosition = serverXform.MapPosition;
            var inRange =
                serverPosition.MapId == report.WorldPosition.MapId &&
                Vector2.DistanceSquared(serverPosition.Position, report.WorldPosition.Position) <=
                wireless.Range * wireless.Range;

            if (!inRange)
            {
                // CullOutOfRange also handles this; only touch dict if we currently track the key.
                if (server.SensorStatus.ContainsKey(key))
                    RemoveSensor(server, key);
                continue;
            }

            // Frame is refreshed on the publish tick; only ensure it exists here.
            if (server.ReferenceFrame == null && !EnsureReferenceFrame(serverUid, server))
                continue;

            if (server.ReferenceFrame == null ||
                !TryGetEntity(server.ReferenceFrame.FrameEntity, out var frameUid))
            {
                continue;
            }

            var now = _timing.CurTime;
            // Always have a frame-local position available so Off/stale entries
            // can keep (or backfill) the last known location.
            var worldLocal = Vector2.Transform(
                report.WorldPosition.Position,
                _transform.GetInvWorldMatrix(frameUid.Value));
            var framedWorldCoords = GetNetCoordinates(
                new EntityCoordinates(frameUid.Value, worldLocal));

            if (report.Status.Mode == SuitSensorMode.SensorOff)
            {
                // Drop from the live set, but never wipe the last-known snapshot.
                // Mode/medical data stay as last reported so the UI can show
                // yellow (stale) with coordinates instead of red (off) with none.
                server.SensorStatus.Remove(key);

                if (!server.LastSensorSnapshot.TryGetValue(key, out var retained))
                    continue;

                retained.IsActive = false;
                retained.Timestamp = now;
                retained.Coordinates ??= framedWorldCoords;
                server.SnapshotDirty = true;
                continue;
            }

            NetCoordinates? framedCoords = report.Status.Coordinates;
            if (report.Status.Mode == SuitSensorMode.SensorCords)
                framedCoords = framedWorldCoords;

            if (server.SensorStatus.TryGetValue(key, out var previous) &&
                SensorStatusMatches(previous, report.Status, framedCoords))
            {
                previous.Timestamp = now;
                if (server.LastSensorSnapshot.TryGetValue(key, out var snap))
                {
                    snap.Timestamp = now;
                    snap.IsActive = true;
                    if (framedCoords != null)
                        snap.Coordinates = framedCoords;
                }
                continue;
            }

            var framedStatus = CopyStatus(report.Status, framedCoords, now);
            framedStatus.IsActive = true;
            server.SensorStatus[key] = framedStatus;
            server.LastSensorSnapshot[key] = CopyStatus(framedStatus, framedCoords, now);
            server.SnapshotDirty = true;
        }
    }

    /// <summary>
    /// Unregisters a console. Idles the server (and may stop global sensor reporting) when none remain.
    /// </summary>
    public void RemoveSubscriber(CrewMonitoringServerComponent server, EntityUid consoleUid)
    {
        if (!server.SubscriberConsoles.Remove(consoleUid))
            return;

        if (server.SubscriberConsoles.Count != 0)
            return;

        _serversWithSubscribers = Math.Max(0, _serversWithSubscribers - 1);
        EnterIdle(server);
    }

    /// <summary>
    /// Drops every subscriber (e.g. server deleting). Stops sensor ingest for this server.
    /// </summary>
    public void ClearSubscribers(CrewMonitoringServerComponent server)
    {
        if (server.SubscriberConsoles.Count == 0)
            return;

        server.SubscriberConsoles.Clear();
        _serversWithSubscribers = Math.Max(0, _serversWithSubscribers - 1);
        EnterIdle(server);
    }

    private void OnMapInit(EntityUid uid, CrewMonitoringServerComponent component, MapInitEvent args)
    {
        component.ServerAddress ??= $"10.0.{_random.Next(256)}.{_random.Next(256)}";
        // Reference frame is built lazily on first subscriber / first report — not at map init.
    }

    private void OnSensorReport(
        Entity<SuitSensorComponent> sensor,
        ref SuitSensorReportEvent report)
    {
        IngestReport(in report);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _publishAccumulator += frameTime;
        if (_publishAccumulator < PublishInterval)
            return;
        _publishAccumulator -= PublishInterval;

        var query = EntityQueryEnumerator<
            CrewMonitoringServerComponent,
            DeviceNetworkComponent,
            ApcPowerReceiverComponent>();

        while (query.MoveNext(out var uid, out var server, out var device, out var power))
        {
            if (!power.Powered)
            {
                if (_deviceNetwork.IsDeviceConnected(uid, device))
                    _deviceNetwork.DisconnectDevice(uid, device, false);
                if (server.SensorStatus.Count > 0)
                    EnterIdle(server);
                continue;
            }

            // Keep device-net presence so consoles can discover/online-check the machine.
            // Sensor ingest + publish only run while someone is subscribed.
            if (!_deviceNetwork.IsDeviceConnected(uid, device))
                _deviceNetwork.ConnectDevice(uid, device);

            if (server.SubscriberConsoles.Count == 0)
            {
                if (server.SensorStatus.Count > 0)
                    EnterIdle(server);
                continue;
            }

            EnsureReferenceFrame(uid, server);
            RemoveTimedOutSensors(server);

            if (TryComp<WirelessNetworkComponent>(uid, out var wireless) &&
                TryComp<TransformComponent>(uid, out var serverXform))
            {
                CullOutOfRangeSensors(server, wireless, serverXform);
            }

            // Heartbeat keeps the console "online"; full payload is the persistent
            // last-known snapshot, not only the currently live sensor set.
            var sendFullSnapshot = server.SnapshotDirty;
            Dictionary<string, SuitSensorStatus>? snapshot = null;
            if (sendFullSnapshot)
            {
                if (server.LastSensorSnapshot.Count == 0)
                {
                    snapshot = EmptySensorSnapshot;
                }
                else
                {
                    snapshot = new Dictionary<string, SuitSensorStatus>(server.LastSensorSnapshot.Count);
                    foreach (var (key, status) in server.LastSensorSnapshot)
                        snapshot[key] = CopyStatus(status, status.Coordinates, status.Timestamp);
                }
            }

            var update = new CrewMonitoringServerUpdateEvent(snapshot);
            RaiseLocalEvent(uid, ref update);

            if (update.Delivered && sendFullSnapshot)
                server.SnapshotDirty = false;
        }
    }

    /// <summary>Shared empty snapshot — must never be mutated.</summary>
    private static readonly Dictionary<string, SuitSensorStatus> EmptySensorSnapshot = new();

    /// <summary>
    /// Creates an isolated copy of the server's persistent last-known snapshot.
    /// </summary>
    public static Dictionary<string, SuitSensorStatus> CopyLastSnapshot(CrewMonitoringServerComponent server)
    {
        var snapshot = new Dictionary<string, SuitSensorStatus>(server.LastSensorSnapshot.Count);
        foreach (var (key, status) in server.LastSensorSnapshot)
            snapshot[key] = CopyStatus(status, status.Coordinates, status.Timestamp);
        return snapshot;
    }

    /// <summary>
    /// Drops only live sensor state while retaining the persistent last-known snapshot.
    /// </summary>
    public static void EnterIdle(CrewMonitoringServerComponent server)
    {
        server.SensorStatus.Clear();

        var changed = false;
        foreach (var status in server.LastSensorSnapshot.Values)
        {
            if (!status.IsActive)
                continue;

            status.IsActive = false;
            changed = true;
        }

        if (changed)
            server.SnapshotDirty = true;
    }

    private void OnRemove(
        EntityUid uid,
        CrewMonitoringServerComponent component,
        ComponentRemove args)
    {
        if (component.SubscriberConsoles.Count > 0)
            _serversWithSubscribers = Math.Max(0, _serversWithSubscribers - 1);

        component.SensorStatus.Clear();
        component.LastSensorSnapshot.Clear();
        component.SubscriberConsoles.Clear();
        component.ReferenceFrame = null;
    }

    /// <summary>
    /// Rebuilds the reference frame only when grid/map, origin, range, or name actually changed.
    /// </summary>
    private bool EnsureReferenceFrame(EntityUid uid, CrewMonitoringServerComponent component)
    {
        var xform = Transform(uid);
        var frameUid = xform.GridUid ?? xform.MapUid;
        if (frameUid == null)
            return false;

        var localOrigin = Vector2.Transform(
            xform.MapPosition.Position,
            _transform.GetInvWorldMatrix(frameUid.Value));
        var origin = GetNetCoordinates(new EntityCoordinates(frameUid.Value, localOrigin));
        var range = TryComp<WirelessNetworkComponent>(uid, out var wireless)
            ? wireless.Range
            : 0f;
        var frameName = xform.GridUid != null
            ? Name(xform.GridUid.Value)
            : "Open Space";
        var netFrame = GetNetEntity(frameUid.Value);

        if (component.ReferenceFrame is { } existing &&
            existing.FrameEntity == netFrame &&
            existing.Range == range &&
            existing.Name == frameName &&
            Nullable.Equals(existing.Origin, origin))
        {
            return true;
        }

        if (component.ReferenceFrame != null &&
            component.ReferenceFrame.FrameEntity != netFrame)
        {
            component.SensorStatus.Clear();
            component.LastSensorSnapshot.Clear();
            component.SnapshotDirty = true;
        }

        component.ReferenceFrame = new CrewMonitoringReferenceFrame(
            netFrame,
            origin,
            range,
            frameName);
        return true;
    }

    private void RemoveTimedOutSensors(CrewMonitoringServerComponent component)
    {
        _removeBuffer.Clear();
        foreach (var (key, status) in component.SensorStatus)
        {
            if ((_timing.CurTime - status.Timestamp).TotalSeconds > component.SensorTimeout)
                _removeBuffer.Add(key);
        }

        foreach (var key in _removeBuffer)
            RemoveSensor(component, key);
    }

    /// <summary>
    /// Drop sensors (including those on moving shuttles/grids) as soon as they leave wireless range,
    /// without waiting for the next suit-sensor report tick.
    /// </summary>
    private void CullOutOfRangeSensors(
        CrewMonitoringServerComponent server,
        WirelessNetworkComponent wireless,
        TransformComponent serverXform)
    {
        var serverPos = serverXform.MapPosition;
        var rangeSq = wireless.Range * wireless.Range;
        _removeBuffer.Clear();

        foreach (var (key, status) in server.SensorStatus)
        {
            if (!TryGetEntity(status.SuitSensorUid, out var sensorUid) ||
                !TryComp(sensorUid.Value, out SuitSensorComponent? sensor) ||
                sensor.User == null ||
                !TryComp(sensor.User.Value, out TransformComponent? wearerXform) ||
                wearerXform.MapID != serverPos.MapId)
            {
                _removeBuffer.Add(key);
                continue;
            }

            var wearerPos = wearerXform.MapPosition;
            if (Vector2.DistanceSquared(serverPos.Position, wearerPos.Position) > rangeSq)
                _removeBuffer.Add(key);
        }

        foreach (var key in _removeBuffer)
            RemoveSensor(server, key);
    }

    private static void RemoveSensor(CrewMonitoringServerComponent component, string key)
    {
        if (!component.SensorStatus.Remove(key))
            return;

        // Keep the last data/coordinates, but mark the retained entry inactive.
        if (component.LastSensorSnapshot.TryGetValue(key, out var retained) && retained.IsActive)
        {
            retained.IsActive = false;
            component.SnapshotDirty = true;
        }
    }

    private static SuitSensorStatus CopyStatus(
        SuitSensorStatus source,
        NetCoordinates? coordinates,
        TimeSpan timestamp)
    {
        // Reuse empty department lists; only copy when non-empty to avoid alloc churn.
        var departments = source.JobDepartments.Count == 0
            ? source.JobDepartments
            : new List<string>(source.JobDepartments);

        return new SuitSensorStatus(
            source.OwnerUid,
            source.SuitSensorUid,
            source.Name,
            source.Job,
            source.JobIcon,
            departments)
        {
            Timestamp = timestamp,
            IsAlive = source.IsAlive,
            TotalDamage = source.TotalDamage,
            TotalDamageThreshold = source.TotalDamageThreshold,
            Coordinates = coordinates,
            Mode = source.Mode,
            IsActive = source.IsActive,
        };
    }

    private static bool SensorStatusMatches(
        SuitSensorStatus existing,
        SuitSensorStatus incoming,
        NetCoordinates? framedCoords)
    {
        if (existing.SuitSensorUid != incoming.SuitSensorUid ||
            existing.OwnerUid != incoming.OwnerUid ||
            existing.Name != incoming.Name ||
            existing.Job != incoming.Job ||
            existing.JobIcon != incoming.JobIcon ||
            existing.IsAlive != incoming.IsAlive ||
            existing.TotalDamage != incoming.TotalDamage ||
            existing.TotalDamageThreshold != incoming.TotalDamageThreshold ||
            !Nullable.Equals(existing.Coordinates, framedCoords) ||
            existing.Mode != incoming.Mode ||
            existing.IsActive != incoming.IsActive ||
            existing.JobDepartments.Count != incoming.JobDepartments.Count)
        {
            return false;
        }

        for (var i = 0; i < existing.JobDepartments.Count; i++)
        {
            if (existing.JobDepartments[i] != incoming.JobDepartments[i])
                return false;
        }

        return true;
    }
}
