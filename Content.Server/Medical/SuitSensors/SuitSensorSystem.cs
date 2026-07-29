using Content.Server.ADT.Medical.SuitSensors;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    // ADT-Tweak Start - New Monitor: idle/wake report pipeline fields
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly CrewMonitoringServerSystem _monitoringServers = default!;

    private static readonly TimeSpan CoordinatesUpdateRate = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Tracks the previous subscriber gate so we can wake every sensor the moment
    /// the first console starts listening (after a long idle period).
    /// </summary>
    private bool _wasReporting;

    /// <summary>
    /// Last reported mode/user/mob-state. MobState is included so crit/dead
    /// transitions bypass the normal UpdateRate timer.
    /// </summary>
    private readonly Dictionary<EntityUid, (SuitSensorMode Mode, EntityUid User, MobState MobState)> _lastReported = new();
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: immediate crit/dead reports
    public override void Initialize()
    {
        base.Initialize();
        // Crit/dead must hit monitors on the same tick as MobState changes —
        // do not wait for the next sensor UpdateRate.
        // Broadcast: cannot SubscribeLocalEvent<MobStateComponent, …> — SharedStunSystem
        // already owns that directed (comp, event) pair.
        SubscribeLocalEvent<MobStateChangedEvent>(OnWearerMobStateChanged);
    }

    private void OnWearerMobStateChanged(MobStateChangedEvent args)
    {
        if (!_monitoringServers.HasAnySubscribers)
            return;

        // Only force-push transitions that involve crit or death (enter or leave).
        if (!IsUrgentMobState(args.OldMobState) && !IsUrgentMobState(args.NewMobState))
            return;

        var wearer = args.Target;
        var query = EntityQueryEnumerator<SuitSensorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var sensorXform))
        {
            if (sensor.User != wearer)
                continue;

            // Off sensors do not stream vitals — nothing useful to push for crit/dead.
            if (sensor.Mode == SuitSensorMode.SensorOff)
                continue;

            TryReportSensor(uid, sensor, sensorXform, force: true);
        }
    }

    private static bool IsUrgentMobState(MobState state) =>
        state is MobState.Critical or MobState.Dead;

    /// <summary>
    /// Clears the report cache and schedules every sensor for an immediate update.
    /// Used when a monitor resets its snapshots and needs a fresh ingest.
    /// </summary>
    public void ForceImmediateReports()
    {
        if (!_monitoringServers.HasAnySubscribers)
            return;

        _lastReported.Clear();
        var now = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SuitSensorComponent>();
        while (query.MoveNext(out _, out var sensor))
            sensor.NextUpdate = now;
    }
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: clear report cache on shutdown
    protected override void OnShutdown(Entity<SuitSensorComponent> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);
        _lastReported.Remove(ent.Owner);
    }
    // ADT-Tweak End

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // ADT-Tweak Start - New Monitor:
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

        var now = _gameTiming.CurTime;
        var wakeAll = !_wasReporting;
        _wasReporting = true;

        var query = EntityQueryEnumerator<SuitSensorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sensor, out var sensorXform))
        {
            if (wakeAll)
                sensor.NextUpdate = now;

            TryReportSensor(uid, sensor, sensorXform, force: false);
        }
        // ADT-Tweak End
    }

    // ADT-Tweak Start - New Monitor:
    /// <summary>
    /// Builds and ingests a suit-sensor report when due.
    /// <paramref name="force"/> skips the UpdateRate gate (used for crit/dead).
    /// </summary>
    private void TryReportSensor(
        EntityUid uid,
        SuitSensorComponent sensor,
        TransformComponent sensorXform,
        bool force)
    {
        if (sensor.User == null ||
            !TryComp<TransformComponent>(sensor.User.Value, out var wearerXform) ||
            wearerXform.MapID == MapId.Nullspace)
        {
            _lastReported.Remove(uid);
            return;
        }

        var mobState = MobState.Invalid;
        if (TryComp<MobStateComponent>(sensor.User.Value, out var mob))
            mobState = mob.CurrentState;

        var reportState = (Mode: sensor.Mode, User: sensor.User.Value, MobState: mobState);
        var hadPrevious = _lastReported.TryGetValue(uid, out var previous);
        var stateChanged = !hadPrevious || previous != reportState;

        // Off is transmitted once per mode/wearer/mob-state change.
        if (sensor.Mode == SuitSensorMode.SensorOff && !stateChanged)
            return;

        var now = _gameTiming.CurTime;
        var urgent = IsUrgentMobState(mobState) ||
                     (hadPrevious && IsUrgentMobState(previous.MobState));

        // Crit/dead transitions (and recovery) never wait on UpdateRate.
        if (!force && !stateChanged && now < sensor.NextUpdate)
            return;

        var updateRate = sensor.Mode == SuitSensorMode.SensorCords
            ? CoordinatesUpdateRate
            : sensor.UpdateRate;
        sensor.NextUpdate = now + updateRate;

        var status = GetSensorState((uid, sensor, sensorXform));
        if (status == null)
            return;

        status.Timestamp = now;
        var report = new SuitSensorReportEvent(
            uid,
            sensor.User.Value,
            status,
            wearerXform.MapPosition);
        _monitoringServers.IngestReport(in report, urgent: force || urgent);
        _lastReported[uid] = reportState;
    }
    // ADT-Tweak End
}
