using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Body.Prosthesis;

[RegisterComponent, NetworkedComponent]
public sealed partial class ProsthesisComponent : Component
{
    [DataField]
    public float SpeedModifier = 1f;

    [DataField]
    public float MeleeModifier = 1f;

    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict =
        {
            ["Blunt"] = 2,
            ["Heat"] = 1.5,
        },
    };

    [DataField]
    public TimeSpan EmpKnockdown = TimeSpan.FromSeconds(5);
}
