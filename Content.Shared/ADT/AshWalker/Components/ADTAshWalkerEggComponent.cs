using Robust.Shared.GameStates;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAshWalkerEggComponent : Component
{
    [DataField]
    public bool Shaman;
}
