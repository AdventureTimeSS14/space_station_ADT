// ADT-Tweak: FILE DISABLED. Upstream CrewMonitoring is commented out - the active
// implementation is the ADT analogue (ADTCrewMonitoring* under Content.*/ADT/Medical/CrewMonitoring).
// Kept commented to stay in sync with upstream, but unused.
/*
using Content.Shared.Medical.SuitSensors;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringServerSystem))]
public sealed partial class CrewMonitoringServerComponent : Component
{

    /// <summary>
    ///     List of all currently connected sensors to this server.
    /// </summary>
    public readonly Dictionary<string, SuitSensorStatus> SensorStatus = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;
}

*/
