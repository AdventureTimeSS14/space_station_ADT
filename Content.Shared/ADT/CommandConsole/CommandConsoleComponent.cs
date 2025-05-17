using Robust.Shared.Utility;

namespace Content.Shared.ADT.CommandConsole;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CommandConsoleComponent : Component
{
    [DataField]
    public string? Input;
}

