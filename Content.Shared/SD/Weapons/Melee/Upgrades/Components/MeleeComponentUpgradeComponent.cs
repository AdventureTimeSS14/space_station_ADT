using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Melee.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(MeleeUpgradeSystem))]
public sealed partial class MeleeComponentUpgradeComponent : Component
{
    [DataField]
    public ComponentRegistry Components = new();
}
