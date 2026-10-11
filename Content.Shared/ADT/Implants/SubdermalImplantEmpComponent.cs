using Content.Shared.Damage;

namespace Content.Shared.ADT.Implants;

[RegisterComponent]
public sealed partial class SubdermalImplantEmpComponent : Component
{
    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new()
        {
            ["Shock"] = 10
        }
    };

    [DataField]
    public float EmpCooldown = 30f;
}
