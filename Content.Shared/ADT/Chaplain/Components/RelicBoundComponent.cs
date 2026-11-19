using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RelicBoundComponent : Component
{
    [DataField]
    public string Slot = "neck";
}
