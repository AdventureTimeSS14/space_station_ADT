namespace Content.Shared.ADT.Bubblegum.Loot;

[RegisterComponent]
public sealed partial class BloodFrenzyPendingComponent : Component
{
    [DataField]
    public TimeSpan ApplyAt;
}
