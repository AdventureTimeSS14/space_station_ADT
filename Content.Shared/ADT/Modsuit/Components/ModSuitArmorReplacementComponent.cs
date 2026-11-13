using Content.Shared.Damage;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent]
public sealed partial class ModSuitArmorReplacementComponent : Component
{
    [DataField]
    public DamageModifierSet? OriginalModifiers;
}