namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent]
public sealed partial class ADTMagmiteScannerComponent : Component
{
    [DataField]
    public float Range = 30f;

    [ViewVariables]
    public EntityUid? User;

    [ViewVariables]
    public List<EntityUid> Chain = new();
}
