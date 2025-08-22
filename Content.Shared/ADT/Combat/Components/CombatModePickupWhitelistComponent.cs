using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CombatModePickupWhitelistComponent : Component
{
    [DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField("blacklist"), AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
