using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Minesweeper;

/// <summary>
/// Component Minesweeper
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MinesweeperComponent : Component
{
    // [DataField]
    // public string? Input;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public string? LastOpenedBy;
}


// TODO: Доделать запись рекордов
// [Serializable, NetSerializable]
// public sealed class MinesweeperRecord
// {
//     public string Difficulty = "";
//     public float TimeSeconds = 0f;
//     public string EntityName = "";

//     public override string ToString()
//     {
//         var time = TimeSpan.FromSeconds(TimeSeconds);
//         return $"{EntityName} — {Difficulty} — {time:mm\\:ss}";
//     }
// }

// [Serializable, NetSerializable]
// public sealed class MinesweeperWinMessage : BoundUserInterfaceMessage
// {
//     public string Difficulty { get; }
//     public float TimeSeconds { get; }

//     public MinesweeperWinMessage(string difficulty, float timeSeconds)
//     {
//         Difficulty = difficulty;
//         TimeSeconds = timeSeconds;
//     }
// }

[Serializable, NetSerializable]
public sealed class OnWinMessage : BoundUserInterfaceMessage
{
    public string? UserName;

    public OnWinMessage(string? userName)
    {
        UserName = userName;
    }
}
