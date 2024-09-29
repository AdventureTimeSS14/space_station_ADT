using Robust.Shared.GameStates;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.OnGhostAttemtpDamage;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class OnGhostAttemtpDamageComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> BloodlossDamageType = "Bloodloss";
}
