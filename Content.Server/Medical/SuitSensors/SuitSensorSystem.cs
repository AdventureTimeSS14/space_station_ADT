using Content.Server.ADT.Medical.SuitSensors;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    // #ADT-Tweak Start - New Monitor: idle/wake report pipeline fields
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CrewMonitoringServerSystem _monitoringServers = default!;

    private static readonly TimeSpan CoordinatesUpdateRate = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Tracks the previous subscriber gate so we can wake every sensor the moment
    /// the first console starts listening (after a long idle period).
    /// </summary>
    private bool _wasReporting;
    private readonly Dictionary<EntityUid, (SuitSensorMode Mode, EntityUid User)> _lastReported = new();
    // #ADT-Tweak End

    // #ADT-Tweak Start - New Monitor: clear report cache on shutdown
    protected override void OnShutdown(Entity<SuitSensorComponent> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);
        _lastReported.Remove(ent.Owner);
    }
    // #ADT-Tweak End

    // #ADT-Tweak Start - New Monitor: IngestReport Update (subscriber-gated)
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // SuitSensorReportEvent is only consumed by crew-monitoring servers.
        // Building statuses every tick with no listeners allocates heavily and
        // shows up as periodic GC frame spikes (~10–20s Gen2 cadence).
        var hasSubscribers = _monitoringServers.HasAnySubscribers;
        if (!hasSubscribers)
        {
            _wasReporting = false;
            _lastReported.Clear();
            return;
        }

        var now = _timing.CurTime;
        var wakeAll = !_wasReporting;
        _wasReporting = true;

        var query = EntityQueryEnumerator<SuitSensorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var sensorXform))
        {
            if (wakeAll)
                sensor.NextUpdate = now;

            if (sensor.User == null ||
                !TryComp<TransformComponent>(sensor.User.Value, out var wearerXform) ||
                wearerXform.MapID == MapId.Nullspace)
            {
                _lastReported.Remove(uid);
                continue;
            }

            var reportState = (Mode: sensor.Mode, User: sensor.User.Value);
            var stateChanged = !_lastReported.TryGetValue(uid, out var previous) ||
                               previous != reportState;

            // Off is transmitted once per mode/wearer change. Active modes keep
            // their normal periodic status updates.
            if (sensor.Mode == SuitSensorMode.SensorOff && !stateChanged)
                continue;

            if (!stateChanged && now < sensor.NextUpdate)
                continue;

            var updateRate = sensor.Mode == SuitSensorMode.SensorCords
                ? CoordinatesUpdateRate
                : sensor.UpdateRate;
            sensor.NextUpdate = now + updateRate;

            var status = GetSensorState((uid, sensor, sensorXform));
            if (status == null)
                continue;

            status.Timestamp = now;
            var report = new SuitSensorReportEvent(
                uid,
                sensor.User.Value,
                status,
                wearerXform.MapPosition);
            _monitoringServers.IngestReport(in report);
            _lastReported[uid] = reportState;
        }
    }
    // #ADT-Tweak End
}
