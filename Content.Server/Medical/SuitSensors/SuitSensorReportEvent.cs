using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map;

namespace Content.Server.Medical.SuitSensors;

[ByRefEvent]
public readonly record struct SuitSensorReportEvent(
    EntityUid Sensor,
    EntityUid Wearer,
    SuitSensorStatus Status,
    MapCoordinates WorldPosition);
