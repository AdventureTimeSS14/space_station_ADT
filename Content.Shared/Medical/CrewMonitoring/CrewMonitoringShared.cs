using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public sealed class CrewMonitoringServerEntry
{
    public NetEntity NetEntity;
    public NetCoordinates? Coordinates;
    public string ServerCode;
    public bool IsOnline;

    public CrewMonitoringServerEntry(NetEntity netEntity, NetCoordinates? coordinates, string serverCode, bool isOnline)
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        ServerCode = serverCode;
        IsOnline = isOnline;
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
    /// Code/ID of the server (e.g. device address). 
    /// </summary>
    public string ServerCode;
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

    public CrewMonitoringState(
        List<SuitSensorStatus> sensors,
        bool isEmagged,
        bool serverOnline,
        string serverName,
        string serverCode,
        string stationCode,
        bool alertActive,
        bool alertMuted,
        List<CrewMonitoringServerEntry>? servers = null)
    {
        Sensors = sensors;
        IsEmagged = isEmagged;
        ServerOnline = serverOnline;
        ServerName = serverName;
        ServerCode = serverCode;
        StationCode = stationCode;
        AlertActive = alertActive;
        AlertMuted = alertMuted;
        Servers = servers ?? new List<CrewMonitoringServerEntry>();
    }
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
