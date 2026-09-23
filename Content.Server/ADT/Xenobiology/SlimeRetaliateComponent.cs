using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeRetaliateComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Attacker;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExpiresAt;

    [ViewVariables(VVAccess.ReadOnly)]
    public float MaxDistance;
}