namespace Content.Shared.ADT.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class NervousSourceComponent : Component
{
    [DataField]
    public float NervousRange = 7;
}
