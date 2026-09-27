namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTDryingRackComponent : Component
{
    [DataField]
    public string Container = "storagebase";

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextUpdate;
}
