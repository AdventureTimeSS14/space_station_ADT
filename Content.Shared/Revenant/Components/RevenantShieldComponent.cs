namespace Content.Shared.Revenant.Components;

[RegisterComponent]
public sealed partial class RevenantShieldComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Used = false;
}
