using Content.Shared.Medical.SuitSensor;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringServerSystem), typeof(CrewMonitoringConsoleSystem))]
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
    ///     Unique address of this server (e.g. "10.0.12.34"). Set in the prototype to distinguish multiple servers.
    ///     If null, a random address is generated at map init.
    /// </summary>
    [DataField("serverAddress"), ViewVariables(VVAccess.ReadWrite)]
    public string? ServerAddress;

    /// <summary>
    /// Consoles that have selected this server (monitor-server pair). Server is active only when this is non-empty.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> SubscriberConsoles = new();
}
