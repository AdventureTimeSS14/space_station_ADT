using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.HolyDamage;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolyDamageComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 55f;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageToVessel")]
    public float DamageToVessel = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageToPuppet")]
    public float DamageToPuppet = 30f;

    [DataField]
    public FixedPoint2 SuppressionFirstHit = 30;

    [DataField]
    public FixedPoint2 SuppressionHit = 15;
}
