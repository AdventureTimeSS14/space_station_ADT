namespace Content.Shared.ADT.Body.OrganGrant;

[RegisterComponent]
public sealed partial class OrganGrantTrackerComponent : Component
{
    [ViewVariables]
    public Dictionary<string, HashSet<EntityUid>> Sources = new();

    [ViewVariables]
    public HashSet<string> Owned = new();
}
