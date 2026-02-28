using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Shared.Audio;
using Robust.Shared.Network;

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;


    // ADT-Tweak-Start
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsEmagged = false;

    /// <summary>
    /// What departments this monitor can see. If empty, shows all departments.
    /// </summary>
    [DataField("departments")]
    public List<CrewMonitoringDepartment> Departments = new();

    /// <summary>
    /// Emag sound effects.
    /// </summary>
    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8),
    };

    /// <summary>
    /// Sound played when any monitored sensor reports crit or dead. Repeats every CritAlertInterval seconds.
    /// </summary>
    [DataField("critAlertSound")]
    public SoundSpecifier CritAlertSound = new SoundPathSpecifier("/Audio/ADT/Machines/crew_monitor_crit_alert.ogg");

    /// <summary>
    /// Interval in seconds between repeated crit/dead alerts.
    /// </summary>
    [DataField("critAlertInterval")]
    public float CritAlertInterval = 20f;

    /// <summary>
    /// Next game time at which to play the crit alert (server-side, not serialized).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextCritAlertTime;

    /// <summary>
    /// Last server name received with sensor data (for UI).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string LastServerName = string.Empty;

    /// <summary>
    /// Last server code received with sensor data (for UI).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string LastServerAddress = string.Empty;

    /// <summary>
    /// If true, crit/dead alert sound is muted on this console.
    /// </summary>
    [DataField("alertMuted"), ViewVariables(VVAccess.ReadWrite)]
    public bool AlertMuted;

    /// <summary>
    /// Last time we received a packet from the server (for connection timeout).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastPacketTime;

    /// <summary>
    /// Cached snapshot of sensors when we had connection; shown when connection is lost.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, SuitSensorStatus> CachedSensors = new();

    /// <summary>
    /// Cached server name/code for display when offline.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string CachedServerName = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string CachedServerAddress = string.Empty;

    /// <summary>
    /// Last grid/station name received (grid where the selected server is).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string LastGridName = string.Empty;

    /// <summary>
    /// Last grid entity received (for map display). Null when server is in space.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity? LastGridUid;

    [ViewVariables(VVAccess.ReadOnly)]
    public string CachedGridName = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity? CachedGridUid;

    /// <summary>
    /// Last time we pushed offline state to UI (throttle).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastOfflineStatePush;

    /// <summary>
    /// Entity that sent the last packet (for server list online status).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LastServerUid;

    /// <summary>
    /// Server this console has selected (monitor-server pair). Null until user selects one.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? SelectedServerUid;

    /// <summary>
    /// True after user has completed "Start scan"; then server list is shown.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasScanned;
    // ADT-Tweak-End
}
