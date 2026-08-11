using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Dice;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class DiceOfFateComponent : Component
{
    [DataField("maxrolls")]
    public int MaxRolls = 10;

    [DataField("rollsleft")]
    [AutoNetworkedField]
    public int RollsLeft = 10;
}

