using Robust.Shared.GameStates;

namespace Content.Shared.ADT.OnGhostAttemtpDamage;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class OnGhostAttemtpDamageComponent : Component
{
    [DataField]
    public string? DamageType = "Bloodloss";
}
