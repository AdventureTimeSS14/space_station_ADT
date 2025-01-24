using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class StaticAnomalyComponent : Component
{
    [DataField]
    public float NoiseRange = 140;

    [DataField]
    public float NoiseStrong = 2;

    [DataField]
    public float MadnessRange = 10;
}
