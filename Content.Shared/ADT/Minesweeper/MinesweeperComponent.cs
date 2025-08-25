using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Minesweeper;

/// <summary>
/// Component Minesweeper
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MinesweeperComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public string? LastOpenedBy;

    [DataField, AutoNetworkedField]
    public List<MinesweeperRecord> Records = new();
}

[Serializable, NetSerializable]
public sealed class MinesweeperRecord
{
    public string Difficulty = string.Empty;
    public float TimeSeconds = 0f;
    public string EntityName = string.Empty;

    public override string ToString()
    {
        var time = TimeSpan.FromSeconds(TimeSeconds);
        var timeStr = time.ToString(@"mm\:ss");
        return Loc.GetString("minesweeper-record-format",
            ("name", EntityName),
            ("difficulty", Difficulty),
            ("time", timeStr));
    }
}

[Serializable, NetSerializable]
public sealed class MinesweeperWinMessage : BoundUserInterfaceMessage
{
    public string Difficulty { get; }
    public float TimeSeconds { get; }
    public string NameWin { get; }

    public MinesweeperWinMessage(string difficulty, float timeSeconds, string nameWin)
    {
        Difficulty = difficulty;
        TimeSeconds = timeSeconds;
        NameWin = nameWin;
    }
}
