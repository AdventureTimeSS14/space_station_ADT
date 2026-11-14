namespace Content.Server.ADT.Heretic.Components;

[RegisterComponent]
public sealed partial class FleshMimicGearComponent : Component
{
    [DataField]
    public List<EntityUid> Gear = new();
}
