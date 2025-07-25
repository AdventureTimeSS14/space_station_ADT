namespace Content.Shared.ADT.StaticTV.Components;

[RegisterComponent]
public sealed partial class StaticTVComponent : Component
{
    [DataField]
    public float Range = 15;

    [DataField]
    public float NoiseStrong = 1f;

    [DataField]
    public float BleedingStrong = 0.03f;

    [DataField]
    public float BloodlossStrong = -0.04f;
}
