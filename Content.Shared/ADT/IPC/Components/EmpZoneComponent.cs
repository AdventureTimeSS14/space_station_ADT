using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;

namespace Content.Shared.ADT.Silicon.Components;

/// <summary>
/// Компонент, который создает зону ЭМИ эффекта с переменной интенсивностью в зависимости от расстояния.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpZoneComponent : Component
{
    /// <summary>
    /// Радиус зоны воздействия в метрах.
    /// </summary>
    [DataField("radius"), AutoNetworkedField]
    public float Radius = 5f;

    /// <summary>
    /// Максимальная интенсивность эффекта (0-1).
    /// </summary>
    [DataField("maxIntensity"), AutoNetworkedField]
    public float MaxIntensity = 1f;

    /// <summary>
    /// Минимальная интенсивность эффекта (0-1).
    /// </summary>
    [DataField("minIntensity"), AutoNetworkedField]
    public float MinIntensity = 0.1f;

    /// <summary>
    /// Длительность эффекта в секундах.
    /// </summary>
    [DataField("duration"), AutoNetworkedField]
    public float Duration = 10f;

    /// <summary>
    /// Включена ли зона.
    /// </summary>
    [DataField("enabled"), AutoNetworkedField]
    public bool Enabled = true;
}
