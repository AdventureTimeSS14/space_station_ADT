using Robust.Shared.GameStates;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAshWalkerComponent : Component
{
    [DataField]
    public bool Shaman;

    [DataField]
    public bool BlockGuns = true;
}
