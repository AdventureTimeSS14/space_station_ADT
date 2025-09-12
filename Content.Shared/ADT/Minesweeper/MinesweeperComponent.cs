using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

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

    [DataField("soundLost")]
    public SoundSpecifier? SoundLost;

    [DataField("soundWin")]
    public SoundSpecifier? SoundWin;

    [DataField("soundTick")]
    public SoundSpecifier? SoundTick;


    /// <summary>
    /// The prototypes that can be dispensed as a reward for winning the game.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("possibleRewards", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> PossibleRewards = new();

    /// <summary>
    /// The minimum number of prizes the arcade machine can have.
    /// </summary>
    [DataField("rewardMinAmount")]
    public int RewardMinAmount;

    /// <summary>
    /// The maximum number of prizes the arcade machine can have.
    /// </summary>
    [DataField("rewardMaxAmount")]
    public int RewardMaxAmount;

    /// <summary>
    /// The remaining number of prizes the arcade machine can dispense.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int RewardAmount = 0;
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

[Serializable, NetSerializable]
public sealed class MinesweeperLostMessage : BoundUserInterfaceMessage
{
    public MinesweeperLostMessage()
    {
    }
}
