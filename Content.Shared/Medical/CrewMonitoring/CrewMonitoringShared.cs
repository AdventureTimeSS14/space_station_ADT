using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public sealed class CrewMonitoringServerEntry
{
    public NetEntity NetEntity;
    public NetCoordinates? Coordinates;
    public string ServerAddress;
    public bool IsOnline;
    /// <summary> Grid/station name where the server is located (for display). </summary>
    public string GridName;

    public CrewMonitoringServerEntry(NetEntity netEntity, NetCoordinates? coordinates, string serverAddress, bool isOnline, string gridName = "")
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        ServerAddress = serverAddress;
        IsOnline = isOnline;
        GridName = gridName ?? string.Empty;
    }
}

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
        NetEntity? selectedServerUid = null)
    {
        Sensors = sensors;
        IsEmagged = isEmagged;
        ServerOnline = serverOnline;
        ServerName = serverName;
        ServerAddress = serverAddress;
        StationCode = stationCode;
        AlertActive = alertActive;
        AlertMuted = alertMuted;
        Servers = servers ?? new List<CrewMonitoringServerEntry>();
        HasScanned = hasScanned;
        GridName = gridName;
        ServerGridUid = serverGridUid;
        SelectedServerUid = selectedServerUid;
    }
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringScanCompleteMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringRescanMessage : BoundUserInterfaceMessage
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


