using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Casino;

[Serializable, NetSerializable]
public enum SlotMachineCasinoUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum SlotMachineCasinoVisuals : byte
{
    Spinning,
    StoppedReels,
    WinBlink,
}

[Serializable, NetSerializable]
public enum SlotMachineCasinoAction : byte
{
    Spin,
    SetBet,
    AutoSpin,
    StopAuto,
    CashOut,
    Gamble,
    Collect,
}

/// <summary>Client -> server actions.</summary>
[Serializable, NetSerializable]
public sealed class SlotMachineCasinoActionMessage : BoundUserInterfaceMessage
{
    public readonly SlotMachineCasinoAction Action;
    public readonly int Amount;
    public readonly string Choice;

    public SlotMachineCasinoActionMessage(SlotMachineCasinoAction action, int amount = 0, string choice = "")
    {
        Action = action;
        Amount = amount;
        Choice = choice;
    }
}

[Serializable, NetSerializable]
public sealed class SlotMachineCasinoBoundUserInterfaceState : BoundUserInterfaceState
{
    public int Money;
    public int Balance;
    public int BetAmount;
    public bool Working;
    public int ActiveLines;
    public int WinningLine;
    public int Plays;
    public int Jackpots;
    public int BigPrizeBase;
    public int SmallPrizeBase;
    public int SpinPriceMin;
    public string ResultMessage = string.Empty;
    public string ResultType = string.Empty;
    public int BonusMultiplier;
    public bool BonusActive;
    public int WinStreak;
    public int LossStreak;
    public int SessionWinnings;
    public bool AutoSpin;
    public int AutoSpinRemaining;
    public int FreeSpinsRemaining;
    public bool GambleAvailable;
    public int GambleAmount;
    public bool NearMiss;
    public string NearMissMessage = string.Empty;
    public bool ShowConfetti;

    /// <summary>[reel][row] symbol ids plus per-reel stopped flags, index-aligned with the component.</summary>
    public string[][] Reels = System.Array.Empty<string[]>();
    public bool[] ReelsStopped = System.Array.Empty<bool>();

    public int SpinCount;

    public List<SlotSpinHistoryEntry> History = new();
}
