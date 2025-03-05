namespace Content.Server._TornadoTech.FriendlyFire;

[RegisterComponent]
public sealed partial class FriendlyFireComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
}
