using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeStoppedComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExpiresAt;
}
