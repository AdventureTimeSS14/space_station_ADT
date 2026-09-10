namespace Content.Shared.ADT.Ghost;

[RegisterComponent]
public sealed partial class GhostVisibleAntagComponent : Component
{
    [DataField]
    public LocId? Name;
}