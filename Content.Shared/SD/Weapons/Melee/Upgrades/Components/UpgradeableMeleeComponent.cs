using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(MeleeUpgradeSystem))]
public sealed partial class UpgradeableMeleeComponent : Component
{
    [DataField("upgradesContainerId")]
    public string UpgradesContainerId = "upgrades";

    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");
}
