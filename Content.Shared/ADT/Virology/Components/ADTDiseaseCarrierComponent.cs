namespace Content.Shared.ADT.Virology.Components;

[RegisterComponent]
public sealed partial class ADTDiseaseCarrierComponent : Component
{
    [ViewVariables]
    public List<ADTDisease> Diseases = new();

    [DataField]
    public HashSet<string> Resistances = new();

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextUpdate;
}
