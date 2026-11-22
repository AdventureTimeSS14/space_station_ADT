using Content.Shared.ADT.Medical.SuitSensors;
using Content.Shared.ADT.Medical.CrewMonitoring;

namespace Content.Server.ADT.Medical.CrewMonitoring;

[RegisterComponent]
// [Access(typeof(ADTCrewMonitoringServerSystem))] ADT-Tweak - New Monitor: ConsoleSystem also accesses server component
[Access(typeof(ADTCrewMonitoringServerSystem), typeof(ADTCrewMonitoringConsoleSystem))]
public sealed partial class ADTCrewMonitoringServerComponent : Component
{
    // ADT-Tweak Start - New Monitor
    /// <summary>
    ///     Live sensors currently in range of this server.
    /// </summary>
    public readonly Dictionary<string, ADTSuitSensorStatus> SensorStatus = new();
    /// <summary>
    /// Last known status for every sensor seen by this server. Unlike
    /// <see cref="SensorStatus"/>, entries are not removed on timeout, range loss,
    /// power loss, or idle, so consoles retain the last known position.
    /// </summary>
    public readonly Dictionary<string, ADTSuitSensorStatus> LastSensorSnapshot = new();
    // ADT-Tweak End

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    // ADT-Tweak Start - New Monitor: 10s -> 3s sensor timeout
    public float SensorTimeout = 3f;
    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: reference frame + server identity
    /// <summary>
    /// Grid or map frame used by all coordinates in the current snapshot.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public ADTCrewMonitoringReferenceFrame? ReferenceFrame;

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
    // ADT-Tweak End
}
