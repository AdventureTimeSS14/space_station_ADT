using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RTGHeatComponent : Component
{
    /// <summary>
    /// Нагрев за секунду
    /// </summary>
    [DataField("heatPerSecond")]
    public float HeatPerSecond = 0.1f;

    /// <summary>
    /// Интервал между нагревом
    /// </summary>
    [DataField("heatInterval")]
    public float HeatInterval = 1f;

    /// <summary>
    /// Максимальная температура нагрева.
    /// </summary>
    [DataField("maxTemperature")]
    public float MaxTemperature = 373.15f;

    public float TimeSinceLastHeat = 0f;
}