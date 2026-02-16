using Content.Shared.Atmos;

namespace Content.Shared.ADT.Temperature;

public sealed class TemperatureImmunityEvent(float currentTemperature) : EntityEventArgs
{
    public float CurrentTemperature = currentTemperature;
    public readonly float IdealTemperature = Atmospherics.T37C;
}

[ByRefEvent]
public record struct BeforeTemperatureChange(
    float CurrentTemperature,
    float LastTemperature,
    float TemperatureDelta);

[ByRefEvent]
public record struct GetTemperatureThresholdsEvent(
    float HeatDamageThreshold,
    float ColdDamageThreshold,
    Dictionary<float, float>? SpeedThresholds);

public sealed class TemperatureChangeAttemptEvent : CancellableEntityEventArgs
{
    public readonly float CurrentTemperature;
    public readonly float LastTemperature;
    public readonly float TemperatureDelta;

    public TemperatureChangeAttemptEvent(float current, float last, float delta)
    {
        CurrentTemperature = current;
        LastTemperature = last;
        TemperatureDelta = delta;
    }
}
