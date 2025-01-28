using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MegafaunaComponent : Component
{
    [DataField]
    public bool Hardmode = false;
}
