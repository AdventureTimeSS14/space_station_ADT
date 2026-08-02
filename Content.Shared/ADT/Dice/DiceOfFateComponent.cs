using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Dice;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class DiceOfFateComponent : Component
{
    [DataField("maxrolls")]
    public int MaxRolls = 10;

    [DataField("rollsused")]
    [AutoNetworkedField]
    public int RollsUsed = 0;
    public int RollsLeft => MaxRolls - RollsUsed;
    public bool HasRollsLeft() => RollsLeft > 0;
}