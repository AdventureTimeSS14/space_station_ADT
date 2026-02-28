using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnMapInit(EntityUid uid, CrewMonitoringServerComponent component, MapInitEvent args)
    {
        if (component.ServerAddress != null)
            return;
        component.ServerAddress = $"10.0.{_random.Next(256)}.{_random.Next(256)}";
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var query = EntityQueryEnumerator<CrewMonitoringServerComponent, DeviceNetworkComponent>();
        while (query.MoveNext(out var uid, out var server, out var device))
        {
            var hasSubscribers = server.SubscriberConsoles.Count > 0;
            var powered = TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered;

            if (powered)
            {
                if (!_deviceNetworkSystem.IsDeviceConnected(uid, device))
                    _deviceNetworkSystem.ConnectDevice(uid, device);
                UpdateTimeout(uid, server);

                // Keep collecting sensor data while powered so selecting a server can show data immediately.
                if (hasSubscribers)
                    BroadcastSensorStatus(uid, server, device);
            }
            else
            {
                if (_deviceNetworkSystem.IsDeviceConnected(uid, device))
                {
                    _deviceNetworkSystem.DisconnectDevice(uid, device, false);
                }
                server.SensorStatus.Clear();
            }
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry only if the sensor is on the same grid as this server.
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var serverGrid = Transform(uid).GridUid;
        var senderGrid = Transform(args.Sender).GridUid;
        if (serverGrid != senderGrid)
            return;

        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        sensorStatus.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var toRemove = new List<string>();
        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.Seconds > component.SensorTimeout)
                toRemove.Add(address);
        }
        foreach (var address in toRemove)
            component.SensorStatus.Remove(address);
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors and the grid/station name where this server is located.
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var serverName = serverComponent.ServerName ?? Name(uid);
        var serverAddress = serverComponent.ServerAddress ?? $"10.0.{_random.Next(256)}.{_random.Next(256)}";
        var xform = Transform(uid);
        var gridUid = xform.GridUid;
        var gridName = gridUid != null ? Name(gridUid.Value) : string.Empty;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = serverComponent.SensorStatus,
            [SuitSensorConstants.NET_SERVER_NAME] = serverName,
            [SuitSensorConstants.NET_SERVER_ADDRESS] = serverAddress,
            [SuitSensorConstants.NET_GRID_NAME] = gridName
        };
        if (gridUid != null)
            payload[SuitSensorConstants.NET_GRID_UID] = GetNetEntity(gridUid.Value);

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }
}
