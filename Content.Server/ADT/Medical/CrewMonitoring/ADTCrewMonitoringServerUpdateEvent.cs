using Content.Shared.ADT.Medical.SuitSensors;

namespace Content.Server.ADT.Medical.CrewMonitoring;

/// <summary>
/// Published by the crew-monitoring server when a sensor snapshot should be
/// delivered to subscribed consoles.
/// </summary>
[ByRefEvent]
public record struct ADTCrewMonitoringServerUpdateEvent(
    Dictionary<string, ADTSuitSensorStatus>? Snapshot,
    bool Delivered = false);
