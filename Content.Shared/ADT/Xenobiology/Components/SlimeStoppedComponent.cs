namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent]
public sealed partial class SlimeStoppedComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExpiresAt;
}
