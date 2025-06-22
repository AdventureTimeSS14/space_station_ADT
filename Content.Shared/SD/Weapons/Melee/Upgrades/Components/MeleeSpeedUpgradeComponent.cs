using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Upgrades.Components;

/// <summary>
/// Компонент для увеличения скорости атаки
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeUpgradeSystem))]
public sealed partial class MeleeSpeedUpgradeComponent : Component
{
    [DataField("speedMultiplier", required: true)]
    public float SpeedMultiplier = 1.0f;
}
