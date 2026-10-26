using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Shared.Audio;
using Robust.Shared.Network;    // ADT-Tweak - New Monitor

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

    // ADT-Tweak Start - New Monitor: alerts, server selection, scan, snapshot pipeline
    /// <summary>
    /// Cached localized department names for this console's filter.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly HashSet<string> CachedDepartmentNames = new();

    /// <summary>
    /// Sound played on crit/dead sensor edges and as a reminder while any remain.
    /// </summary>
    [DataField("critAlertSound")]
    public SoundSpecifier CritAlertSound = new SoundPathSpecifier("/Audio/ADT/Machines/crew_monitor_crit_alert.ogg");

    /// <summary>
    /// Reminder ping interval while at least one sensor is still crit or dead.
    /// Edge transitions fire immediately and reset this timer.
    /// </summary>
    [DataField("critAlertInterval")]
    public float CritAlertInterval = 30f;

    /// <summary>
    /// Next game time for the reminder ping (Zero = no active alert condition).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextCritAlertTime;

    /// <summary>
    /// Wearers already signaled as alerting: value true = dead, false = MobState.Critical.
    /// Edge sound only on new keys or crit -> dead; dead -> crit is silent.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<NetEntity, bool> KnownAlertStates = new();

    /// <summary>
    /// After Reset Sensors: mute edge/reminder beeps until
    /// CritAlertResyncReadyAt, then play one baseline beep if needed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool CritAlertResyncPending;

    /// <summary>
    /// Game time when post-reset sensor re-ingest is considered done.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CritAlertResyncReadyAt;

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
    /// If true, this console never emits the crit/dead alert sound.
    /// World monitors still play; ghosts near them can hear as usual.
    /// </summary>
    [DataField("suppressCritAlertSound")]
    public bool SuppressCritAlertSound;

    /// <summary>
    /// If true, crit/dead alert sound is muted on this console.
    /// </summary>
    [DataField("alertMuted"), ViewVariables(VVAccess.ReadWrite)]
    public bool AlertMuted;

    /// <summary>
    /// Crit/dead alert loudness from 0 (silent) to 1 (prototype volume).
    /// </summary>
    [DataField("alertVolume"), ViewVariables(VVAccess.ReadWrite)]
    public float AlertVolume = 1f;

    /// <summary>
    /// Last time we received a packet from the server (for connection timeout).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastPacketTime;

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

    /// <summary>
    /// Last coordinate frame supplied by the selected monitoring server.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public CrewMonitoringReferenceFrame? LastReferenceFrame;

    /// <summary>
    /// Whether the current offline state was already sent to the UI.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool OfflineStateSent;

    /// <summary>
    /// Entity that sent the last packet (for server list online status).
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LastServerUid;

    /// <summary>
    /// Grids that already received a full NavMap rebuild for this console session
    /// (walls/windows for shuttles and station frames).
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> PopulatedNavMapGrids = new();

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

    /// <summary>
    /// Server time at which the current scan was started.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? ScanStartedAt;

    /// <summary>
    /// Servers discovered by the last scan/rescan. New servers are not added until rescan.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<CrewMonitoringServerEntry> CachedServers = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastServersRefresh;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool ServersListDirty = true;
    // ADT-Tweak End
}
