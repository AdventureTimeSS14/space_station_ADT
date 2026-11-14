namespace Content.Shared.ADT.Virology.Components;

[RegisterComponent]
public sealed partial class ADTDiseaseViewerComponent : Component
{
    [DataField]
    public bool IgnoreStealth = true;
}
