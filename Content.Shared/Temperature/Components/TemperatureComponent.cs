using Content.Shared.Atmos;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles changing temperature,
/// informing others of the current temperature.
/// </summary>
[RegisterComponent]
public sealed partial class TemperatureComponent : Component
{
    /// <summary>
    /// Surface temperature which is modified by the environment.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CurrentTemperature = Atmospherics.T20C;

    /// <summary>
    /// Heat capacity per kg of mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpecificHeat = 50f;

    /// <summary>
    /// How well does the air surrounding you merge into your body temperature?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AtmosTemperatureTransferEfficiency = 0.1f;

    // ADT start - Speed modifiers based on temperature
    /// <summary>
    /// Temperature thresholds and their corresponding speed modifiers.
    /// Key is the temperature threshold, Value is the speed modifier.
    /// </summary>
    [DataField]
    public Dictionary<float, float> Thresholds = new();

    /// <summary>
    /// When the next slowdown update should occur.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? NextSlowdownUpdate;

    /// <summary>
    /// Current speed modifier to apply.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float? CurrentSpeedModifier;
    // ADT end
}
