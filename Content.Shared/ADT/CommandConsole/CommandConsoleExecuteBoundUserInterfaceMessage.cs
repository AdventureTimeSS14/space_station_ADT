using Robust.Shared.Serialization;

namespace Content.Shared.ADT.CommandConsole;


[Serializable, NetSerializable]
public sealed class CommandConsoleExecuteBoundUserInterfaceMessage : BoundUserInterfaceMessage
{
    public string CommandInputText { get; set; } = string.Empty;

    public CommandConsoleExecuteBoundUserInterfaceMessage(string commandInputText)
    {
        CommandInputText = commandInputText;
    }
}
