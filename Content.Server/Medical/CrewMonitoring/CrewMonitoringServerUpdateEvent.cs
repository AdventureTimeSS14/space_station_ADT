using Content.Shared.Medical.SuitSensor;

namespace Content.Server.Medical.CrewMonitoring;

[ByRefEvent]
public record struct CrewMonitoringServerUpdateEvent(
    Dictionary<string, SuitSensorStatus>? Snapshot,
    bool Delivered = false);
