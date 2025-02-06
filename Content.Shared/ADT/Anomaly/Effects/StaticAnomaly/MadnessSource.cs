namespace Content.Shared.ADT.Anomaly.Effects.Components;

[RegisterComponent]
public sealed partial class MadnessSourceComponent : Component
{
    [DataField]
    public float MadnessRange = 7;
}
