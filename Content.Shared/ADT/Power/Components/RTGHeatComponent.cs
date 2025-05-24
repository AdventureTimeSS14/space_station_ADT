using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RTGHeatComponent : Component
{
    /// <summary>
    /// Нагрев за секунду
    /// </summary>
    [DataField("heatPerSecond")]
    public float HeatPerSecond;

    /// <summary>
    /// Интервал между нагревом
    /// </summary>
    [DataField("heatInterval")]
    public float HeatInterval;

    /// <summary>
    /// Максимальная температура нагрева.
    /// </summary>
    [DataField("maxTemperature")]
    public float MaxTemperature;
}