namespace Content.Client.ADT.Drawing.Input;

public sealed class PenInputSystem : EntitySystem
{
    public PressureCurve Curve { get; set; } = PressureCurve.Linear;

    public SimulatedPressure SimulationTemplate { get; } = new();

    public bool SimulatePressure { get; set; } = true;
}
