using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Content.Shared.Weapons.Marker;

namespace Content.Shared.Weapons.Melee.Upgrades.Components;

/// <summary>
/// Компонент для улучшения оружия ближнего боя эффектом кровавого опьянения
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeUpgradeSystem))]
public sealed partial class MeleeBloodDrunkerUpgradeComponent : Component
{

}
