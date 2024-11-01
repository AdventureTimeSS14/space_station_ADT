using Robust.Shared.GameStates;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingHeadslugContainerComponent : Component
{
    public Container? Container;
}
