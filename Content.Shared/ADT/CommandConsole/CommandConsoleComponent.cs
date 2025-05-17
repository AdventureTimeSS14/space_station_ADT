using Robust.Shared.Utility;

namespace Content.Shared.ADT.CommandConsole;

[RegisterComponent]
public sealed partial class CommandConsoleComponent : Component
{
    [DataField]
    public string? Input;

    [DataField]
    public Directory RootDirectory = new() { Name = "" };

    [DataField]
    public string CurrentPath = "/";
}
