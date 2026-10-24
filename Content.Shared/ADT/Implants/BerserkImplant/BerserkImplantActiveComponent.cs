using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.BerserkImplant;

[RegisterComponent, NetworkedComponent]
public sealed partial class BerserkImplantActiveComponent : Component
{
    [DataField]
    public DamageModifierSet DamageModifier = new()
    {
        Coefficients = new()
        {
            { "Slash", 0.25f },
            { "Piercing", 0.25f },
            { "Blunt", 0.25f },
            { "Heat", 0.25f },
            { "Cold", 0.25f },
            { "Shock", 0.25f },
            { "Caustic", 0.25f },
            { "Poison", 0.25f },
            { "Radiation", 0.25f },
            { "Cellular", 0.25f },
            { "Asphyxiation", 0.25f },
            { "Bloodloss", 0.25f },
        }
    };

    [DataField]
    public float StunModifier = 0.5f;

    [DataField]
    public float SelfDamageModifier = 1.5f;

    [DataField]
    public float DelayedDamageModifier = 0.1f;

    public DamageSpecifier DelayedDamage = new();
}
