namespace Content.Shared.ADT.Bubblegum.Loot;

[RegisterComponent]
public sealed partial class BloodContractCleaverComponent : Component
{
    [DataField]
    public EntityUid Victim;
}
