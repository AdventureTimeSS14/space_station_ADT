using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.FiringPin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FiringPinAlertLevelCacheComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CurrentLevel = string.Empty;
}