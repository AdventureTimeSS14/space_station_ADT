//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FleshGhoulLimitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Limit = 5;
}
