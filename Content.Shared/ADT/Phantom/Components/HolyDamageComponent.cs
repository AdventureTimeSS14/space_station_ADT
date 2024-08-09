using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Phantom.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolyDamageComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 55f;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageToVessel")]
    public float DamageToVessel = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageToPuppet")]
    public float DamageToPuppet = 30f;
}
