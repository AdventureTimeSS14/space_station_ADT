using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.CrewMonitoring;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringServerSystem), typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringServerComponent : Component
{

    /// <summary>
    ///     Live sensors currently in range of this server.
    /// </summary>
    public readonly Dictionary<string, SuitSensorStatus> SensorStatus = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 3f;

    /// <summary>
    /// Grid or map frame used by all coordinates in the current snapshot.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public CrewMonitoringReferenceFrame? ReferenceFrame;

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
    /// Consoles that have selected this server. When empty the server does not
    /// ingest sensor reports, cull, or publish snapshots.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> SubscriberConsoles = new();

    /// <summary>
    /// Whether sensor data changed since the last full snapshot.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool SnapshotDirty = true;
}
