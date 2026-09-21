using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HealOnHoldingBlockedComponent : Component
{
    [DataField("expires")]
    [AutoNetworkedField]
    public TimeSpan Expires;
}
