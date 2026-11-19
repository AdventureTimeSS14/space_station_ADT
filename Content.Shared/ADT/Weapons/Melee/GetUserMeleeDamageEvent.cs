using Content.Shared.Damage;

namespace Content.Shared.ADT.Weapons.Melee;

[ByRefEvent]
public record struct GetUserMeleeDamageEvent(EntityUid Weapon, List<DamageModifierSet> Modifiers);
