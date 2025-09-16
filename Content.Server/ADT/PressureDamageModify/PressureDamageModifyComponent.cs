using Content.Shared.Damage;

namespace Content.Server.ADT.PressureDamageModify;

[RegisterComponent]
public sealed partial class PressureDamageModifyComponent : Component
{
    /// <summary>
    /// only for projectiles, doesn`t work for melee damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("projDamage")]
    public float ProjDamage = 0.2f;

    /// <summary>
    /// KPd, below which damage will diminishes.  0 kPa = 1 kPa
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("needsPressure")]
    public float Pressure = 40f;

    /// <summary>
    /// only for melee, doesn`t work for projectiles
    /// </summary>
    [DataField("additionalDamage")]
    public DamageSpecifier? AdditionalDamage = null;
}
