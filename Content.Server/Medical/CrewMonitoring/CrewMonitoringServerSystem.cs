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
    /// Registers a console as listening to this server. Wakes sensor ingest on first subscriber.
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

            NetCoordinates? framedCoords = report.Status.Coordinates;
            if (report.Status.Mode == SuitSensorMode.SensorCords)
            {
                var localPosition = Vector2.Transform(
                    report.WorldPosition.Position,
                    _transform.GetInvWorldMatrix(frameUid.Value));
                framedCoords = GetNetCoordinates(
                    new EntityCoordinates(frameUid.Value, localPosition));
            }

            var now = _timing.CurTime;
            if (server.SensorStatus.TryGetValue(key, out var previous) &&
                SensorStatusMatches(previous, report.Status, framedCoords))
            {
                previous.Timestamp = now;
                continue;
            }

            var framedStatus = CopyStatus(report.Status, framedCoords, now);
            server.SensorStatus[key] = framedStatus;
            server.SnapshotDirty = true;
        }
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

            // Heartbeat keeps the console "online"; full payload only when data changed.
            // Avoid allocating an empty dictionary copy when the snapshot is empty.
            var sendFullSnapshot = server.SnapshotDirty;
            Dictionary<string, SuitSensorStatus>? snapshot = null;
            if (sendFullSnapshot)
            {
                snapshot = server.SensorStatus.Count == 0
                    ? EmptySensorSnapshot
                    : new Dictionary<string, SuitSensorStatus>(server.SensorStatus);
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
    /// Drops any cached sensor state so an idle server holds nothing and does no cull/timeout work.
    /// </summary>
    public static void EnterIdle(CrewMonitoringServerComponent server)
    {
        if (server.SensorStatus.Count == 0)
            return;

        server.SensorStatus.Clear();
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
        if (component.SensorStatus.Remove(key))
            component.SnapshotDirty = true;
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
