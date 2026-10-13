using Content.Shared.ADT.Medical.SuitSensors;
using Robust.Shared.Map;

namespace Content.Server.ADT.Medical.SuitSensors;

/// <summary>
/// Local suit-sensor status report for crew-monitoring servers
/// (replaces continuous device-network packets while idle).
/// </summary>
[ByRefEvent]
public readonly record struct ADTSuitSensorReportEvent(
    EntityUid Sensor,
    EntityUid Wearer,
    ADTSuitSensorStatus Status,
    MapCoordinates WorldPosition);
