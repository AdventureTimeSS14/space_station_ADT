namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent]
public sealed partial class ADTMagmiteScannerLinkComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Scanners = new();
}
