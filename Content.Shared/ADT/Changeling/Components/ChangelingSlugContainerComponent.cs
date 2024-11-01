using Robust.Shared.GameStates;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingHeadslugContainerComponent : Component
{
    [AutoNetworkedField]
    public Container? Container;
}
