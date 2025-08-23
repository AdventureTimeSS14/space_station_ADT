using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

/// <summary>
/// Компонент, ограничивающий возможность поднимать предметы, 
/// когда существо находится в боевом режиме.
/// 
/// Работает по системе whitelist/blacklist:
/// - Если задан <see cref="Whitelist"/>, поднимать можно только предметы из этого списка.
/// - Если задан <see cref="Blacklist"/>, поднимать нельзя предметы из этого списка.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CombatModePickupWhitelistComponent : Component
{
    [DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField("blacklist"), AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
