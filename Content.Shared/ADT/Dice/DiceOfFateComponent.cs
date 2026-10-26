using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Dice;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiceOfFateComponent : Component
{
    [DataField]
    public bool Used;
}