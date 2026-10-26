using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Medical.CrewMonitoring;

// ADT-Tweak Start - New Monitor: server reference frame for framed coordinates
[Serializable, NetSerializable]
public sealed class CrewMonitoringReferenceFrame
{
    public NetEntity FrameEntity;
    public NetCoordinates Origin;
    public float Range;
    public string Name;

    public CrewMonitoringReferenceFrame(
        NetEntity frameEntity,
        NetCoordinates origin,
        float range,
        string name)
    {
        FrameEntity = frameEntity;
        Origin = origin;
        Range = range;
        Name = name;
    }
}
// ADT-Tweak End

// ADT-Tweak Start - New Monitor: server list entry for scan/select UI
[Serializable, NetSerializable]
public sealed class CrewMonitoringServerEntry
{
    public NetEntity NetEntity;
    public NetCoordinates? Coordinates;
    public string ServerAddress;
    public bool IsOnline;
    public float SensorRange;
    /// <summary> Grid/station name where the server is located (for display). </summary>
    public string GridName;

    public CrewMonitoringServerEntry(
        NetEntity netEntity,
        NetCoordinates? coordinates,
        string serverAddress,
        bool isOnline,
        float sensorRange,
        string gridName = "")
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        ServerAddress = serverAddress;
        IsOnline = isOnline;
        SensorRange = sensorRange;
        GridName = gridName ?? string.Empty;
    }
}
// ADT-Tweak End

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

// ADT-Tweak-Start
[Serializable, NetSerializable]
public enum CrewMonitoringDepartment
{
    Cargo,
    Civilian,
    CentralCommand,
    Command,
    Engineering,
    Medical,
    Security,
    Science,
    Silicon,
    Specific
}
// ADT-Tweak-End

[Serializable, NetSerializable]
public sealed class CrewMonitoringState : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors;
    public bool IsEmagged; // ADT-Tweak

    // ADT-Tweak Start - New Monitor: BUI state fields (online, alerts, servers, scan, frame)
    /// <summary>
    /// True if console is receiving data from a server.
    /// </summary>
    public bool ServerOnline;
    /// <summary>
    /// Name of the server we receive sensor data from.
    /// </summary>
    public string ServerName;
    /// <summary>
    /// Address of the server (e.g. "10.0.12.34").
    /// </summary>
    public string ServerAddress;
    /// <summary>
    /// Station code where sensors are located.
    /// </summary>
    public string StationCode;
    /// <summary>
    /// True when any monitored sensor is crit or dead (alert condition).
    /// </summary>
    public bool AlertActive;
    /// <summary>
    /// User has muted the crit/dead alert sound.
    /// </summary>
    public bool AlertMuted;

    /// <summary>
    /// Alert sound volume from 0 (silent) to 1 (full).
    /// </summary>
    public float AlertVolume;

    /// <summary>
    /// All sensor servers on the station for the "Серверы датчиков" department and map blips.
    /// </summary>
    public List<CrewMonitoringServerEntry> Servers;

    /// <summary>
    /// True after the user has completed "Start scan" (progress bar); then server list and tabs are shown.
    /// </summary>
    public bool HasScanned;

    /// <summary>
    /// Name of the grid/station where the selected server is located (for map caption).
    /// </summary>
    public string GridName;

    /// <summary>
    /// Grid entity where the selected server is located; map shows this grid. Null when no server selected.
    /// </summary>
    public NetEntity? ServerGridUid;

    /// <summary>
    /// Server this console is currently connected to (for status: green=connected, yellow=standby, red=offline).
    /// </summary>
    public NetEntity? SelectedServerUid;

    /// <summary>
    /// Coordinate frame and circular coverage area of the selected server.
    /// </summary>
    public CrewMonitoringReferenceFrame? ReferenceFrame;

    public CrewMonitoringState(
        List<SuitSensorStatus> sensors,
        bool isEmagged,
        bool serverOnline,
        string serverName,
        string serverAddress,
        string stationCode,
        bool alertActive,
        bool alertMuted,
        List<CrewMonitoringServerEntry>? servers = null,
        bool hasScanned = false,
        string gridName = "",
        NetEntity? serverGridUid = null,
        NetEntity? selectedServerUid = null,
        CrewMonitoringReferenceFrame? referenceFrame = null,
        float alertVolume = 1f)
    {
        Sensors = sensors;
        IsEmagged = isEmagged;
        ServerOnline = serverOnline;
        ServerName = serverName;
        ServerAddress = serverAddress;
        StationCode = stationCode;
        AlertActive = alertActive;
        AlertMuted = alertMuted;
        AlertVolume = Math.Clamp(alertVolume, 0f, 1f);
        Servers = servers ?? new List<CrewMonitoringServerEntry>();
        HasScanned = hasScanned;
        GridName = gridName;
        ServerGridUid = serverGridUid;
        SelectedServerUid = selectedServerUid;
        ReferenceFrame = referenceFrame;
        // ADT-Tweak End
    }
}

// ADT-Tweak Start - New Monitor: scan/select/alert BUI messages
[Serializable, NetSerializable]
public sealed class CrewMonitoringScanStartMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringScanCompleteMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringRescanMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Clears retained sensor snapshots and forces a fresh ingest from suit sensors.
/// </summary>
[Serializable, NetSerializable]
public sealed class CrewMonitoringResetSensorsMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringSetAlertMutedMessage : BoundUserInterfaceMessage
{
    public bool Muted { get; }

    public CrewMonitoringSetAlertMutedMessage(bool muted)
    {
        Muted = muted;
    }
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringSetAlertVolumeMessage : BoundUserInterfaceMessage
{
    public float Volume { get; }

    public CrewMonitoringSetAlertVolumeMessage(float volume)
    {
        Volume = Math.Clamp(volume, 0f, 1f);
    }
}

/// <summary>
/// Request to make the given server the active one for this station (monitor-server pair).
/// </summary>
[Serializable, NetSerializable]
public sealed class CrewMonitoringSelectServerMessage : BoundUserInterfaceMessage
{
    public NetEntity Server { get; }

    public CrewMonitoringSelectServerMessage(NetEntity server)
    {
        Server = server;
    }
}
// ADT-Tweak End
