using Content.Shared.Medical.SuitSensor;

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

    /// <summary>
    ///     Display name of this server (e.g. for crew monitor UI). If null, entity name is used.
    /// </summary>
    [DataField("serverName"), ViewVariables(VVAccess.ReadWrite)]
    public string? ServerName;

    /// <summary>
    ///     Unique code of this server (e.g. "10.0.0.1"). Set in the prototype to distinguish multiple servers.
    ///     If null, a code is generated at runtime from entity uid.
    /// </summary>
    [DataField("serverCode"), ViewVariables(VVAccess.ReadWrite)]
    public string? ServerCode;
}
