using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringState : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors;
    public bool IsEmagged; // ADT-Tweak

    public CrewMonitoringState(List<SuitSensorStatus> sensors, bool isEmagged) // ADT-Tweak
    {
        Sensors = sensors;
        IsEmagged = isEmagged; // ADT-Tweak
    }
}
