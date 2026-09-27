using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SmolderingHuskComponent : Component
{
    [DataField]
    public DamageModifierSet ModifierSet = new()
    {
        Coefficients =
        {
            { "Radiation", 0.5f },
        },
    };
}
