using Content.Server.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CrewMonitoringServerSystem _monitoringServers = default!;

    private static readonly TimeSpan CoordinatesUpdateRate = TimeSpan.FromSeconds(0.5);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // SuitSensorReportEvent is only consumed by crew-monitoring servers.
        // Building statuses every tick with no listeners allocates heavily and
        // shows up as periodic GC frame spikes (~10–20s Gen2 cadence).
        if (!_monitoringServers.HasAnySubscribers)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SuitSensorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var sensorXform))
        {
            if (now < sensor.NextUpdate)
                continue;

            var updateRate = sensor.Mode == SuitSensorMode.SensorCords
                ? CoordinatesUpdateRate
                : sensor.UpdateRate;
            sensor.NextUpdate = now + updateRate;

            // Off / unworn sensors must not allocate SuitSensorStatus.
            if (sensor.Mode == SuitSensorMode.SensorOff ||
                sensor.User == null ||
                !TryComp<TransformComponent>(sensor.User.Value, out var wearerXform) ||
                wearerXform.MapID == MapId.Nullspace)
            {
                continue;
            }

            var status = GetSensorState((uid, sensor, sensorXform));
            if (status == null)
                continue;

            status.Timestamp = now;
            var report = new SuitSensorReportEvent(
                uid,
                sensor.User.Value,
                status,
                wearerXform.MapPosition);
            RaiseLocalEvent(uid, ref report);
        }
    }
}
