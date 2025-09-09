using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Shared.Audio;

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
    // ADT-Tweak-End
}
