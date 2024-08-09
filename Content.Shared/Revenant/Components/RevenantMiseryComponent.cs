namespace Content.Shared.Revenant.Components;

[RegisterComponent]
public sealed partial class RevenantMiseryComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public EntityUid Event = EntityUid.Invalid;
}
