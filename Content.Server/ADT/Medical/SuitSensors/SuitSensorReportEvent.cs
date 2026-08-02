using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map;

namespace Content.Server.ADT.Medical.SuitSensors;

/// <summary>
/// Local suit-sensor status report for crew-monitoring servers
/// (replaces continuous device-network packets while idle).
/// </summary>
[ByRefEvent]
public readonly record struct SuitSensorReportEvent(
    EntityUid Sensor,
    EntityUid Wearer,
    SuitSensorStatus Status,
    MapCoordinates WorldPosition);
