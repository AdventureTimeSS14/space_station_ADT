using Content.Shared.Damage;

namespace Content.Server.ADT.PressureDamageModify;

[RegisterComponent]
public sealed partial class PressureDamageModifyComponent : Component
{
    /// <summary>
    /// only for projectiles, doesn`t work for melee damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("projDamage")]
    public float ProjDamage = 0.1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("maxPressure")]
    public float MaxPressure = 50f;

    [ViewVariables(VVAccess.ReadWrite), DataField("minPressure")]
    public float MinPressure = -1f;

    /// <summary>
    /// only for melee, doesn`t work for projectiles
    /// </summary>
    [DataField("additionalDamage")]
    public DamageSpecifier? AdditionalDamage = null;
}
