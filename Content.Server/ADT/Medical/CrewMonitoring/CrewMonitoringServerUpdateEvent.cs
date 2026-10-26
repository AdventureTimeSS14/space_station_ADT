using Content.Shared.Medical.SuitSensor;

namespace Content.Server.ADT.Medical.CrewMonitoring;

/// <summary>
/// Published by the crew-monitoring server when a sensor snapshot should be
/// delivered to subscribed consoles.
/// </summary>
[ByRefEvent]
public record struct CrewMonitoringServerUpdateEvent(
    Dictionary<string, SuitSensorStatus>? Snapshot,
    bool Delivered = false);
