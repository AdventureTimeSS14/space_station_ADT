namespace Content.Shared.ADT.Virology.Components;

[RegisterComponent]
public sealed partial class ADTVirusProtectionComponent : Component
{
    [DataField]
    public float Permeability = 1f;

    [DataField]
    public List<string> Zones = new();

    [DataField]
    public float? Airborne;
}
