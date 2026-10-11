using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.BerserkImplant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BerserkImplantActiveComponent : Component
{
    [AutoNetworkedField]
    public float Duration = 8f;

    public DamageModifierSet DamageModifier = new()
    {
        Coefficients = new()
        {
            { "Slash", 0.4f },
            { "Piercing", 0.4f },
            { "Blunt", 0.4f },
            { "Heat", 0.4f },
            { "Shock", 0.4f },
        }
    };

    public float StunModifier = 0.5f;

    public float SelfDamageModifier = 1.5f;

    public float DelayedDamageModifier = 0.2f;

    public DamageSpecifier DelayedDamage = new();

    [AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;
}
