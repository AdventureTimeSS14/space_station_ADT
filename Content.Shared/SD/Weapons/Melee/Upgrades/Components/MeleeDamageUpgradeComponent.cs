using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Upgrades.Components;

/// <summary>
/// Компонент для увеличения урона оружия ближнего боя
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeUpgradeSystem))]
public sealed partial class MeleeDamageUpgradeComponent : Component
{
    [DataField("damageBonus", required: true)]
    public DamageSpecifier DamageBonus = default!;
}
