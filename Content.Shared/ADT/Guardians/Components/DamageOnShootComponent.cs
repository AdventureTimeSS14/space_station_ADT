using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Guardians.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamageOnShootComponent : Component
{
    [DataField("damageAmount")]
    public float DamageAmount = 10f;

    [DataField("damageType")]
    public string DamageType = "Bloodloss";
}