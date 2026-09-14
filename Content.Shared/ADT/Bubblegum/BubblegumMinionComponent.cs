namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent]
public sealed partial class BubblegumMinionComponent : Component
{
    [DataField]
    public EntityUid? Summoner;
}
