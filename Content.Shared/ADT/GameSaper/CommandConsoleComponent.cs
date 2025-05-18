using Robust.Shared.Utility;

namespace Content.Shared.ADT.Minesweeper;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class MinesweeperComponent : Component
{
    [DataField]
    public string? Input;
}
