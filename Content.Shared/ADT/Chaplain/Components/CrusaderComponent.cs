using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrusaderComponent : Component
{
    [DataField]
    public bool Master;

    [ViewVariables]
    public bool Briefed;
}
