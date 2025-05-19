using Robust.Shared.Serialization;

namespace Content.Shared.ADT.CommandConsole;


[Serializable, NetSerializable]
public sealed class MinesweeperExecuteBoundUserInterfaceMessage : BoundUserInterfaceMessage
{
    public string CommandInputText { get; set; } = string.Empty;

    public MinesweeperExecuteBoundUserInterfaceMessage(string commandInputText)
    {
        CommandInputText = commandInputText;
    }
}
