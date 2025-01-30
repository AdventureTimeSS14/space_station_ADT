namespace Content.Shared.ADT.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class StaticAnomalyComponent : Component
{
    [DataField]
    public float NoiseRange = 12;

    [DataField]
    public float NoiseStrong = 0.45f;
}
