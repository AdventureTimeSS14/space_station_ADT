using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Casino;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlotMachineCasinoComponent : Component
{
    // === Shared constants (used by both server logic and client rendering) ===

    /// <summary>Symbol ids in scroll-strip order; keys of <see cref="SymbolWeights"/>.</summary>
    public static readonly string[] SymbolIds =
    {
        "seven", "cherry", "banana", "strawberry", "grape", "diamond", "star", "watermelon", "wild", "scatter",
    };

    /// <summary>Payline patterns: row index (0-based) per column. Index = line number - 1.</summary>
    public static readonly int[][] Paylines =
    {
        new[] { 1, 1, 1, 1, 1 }, // L1 middle
        new[] { 0, 0, 0, 0, 0 }, // L2 top
        new[] { 2, 2, 2, 2, 2 }, // L3 bottom
        new[] { 0, 1, 2, 1, 0 }, // L4 V
        new[] { 2, 1, 0, 1, 2 }, // L5 inverted V
    };

    // === Live state ===

    [DataField, AutoNetworkedField]
    public int Money = 25000;

    public int SpinCount;

    [DataField, AutoNetworkedField]
    public int Balance;

    [DataField, AutoNetworkedField]
    public int BetAmount = 5;

    [DataField, AutoNetworkedField]
    public bool Working;

    [DataField, AutoNetworkedField]
    public int StoppedReels;

    [DataField, AutoNetworkedField]
    public bool ShowWinOverlay;

    // === Server-side only state ===

    public int WinStreak;
    public int LossStreak;
    public int SessionWinnings;

    /// <summary>Per-reel spinning flags, index 0-4. Server side.</summary>
    public readonly bool[] ReelSpinning = new bool[5];

    /// <summary>Reel contents, [reel 0-4][row 0-2], row 1 (middle) is the main payline row.</summary>
    public readonly string[][] Reels = new string[5][];

    public string ResultMessage = string.Empty;
    public string ResultType = string.Empty; // jackpot | win | small | bonus | lose

    public int BonusMultiplier = 1;
    public bool BonusActive;
    public bool AutoSpin;
    public int AutoSpinRemaining;
    public int FreeSpinsRemaining;
    public bool GambleAvailable;
    public int GambleAmount;
    public bool NearMiss;
    public string NearMissMessage = string.Empty;
    public bool ShowConfetti;
    public int ActiveLines = 1;
    public int WinningLine;

    public readonly List<SlotSpinHistoryEntry> SpinHistory = new();
    public TimeSpan SpinStartTime;
    public TimeSpan NextReelStopTime;
    public int NextReelToStop;
    public TimeSpan NextAutoSpinTime;
    public TimeSpan WinBlinkEndTime;
    public float MoneyRemainder;

    [DataField] public int SpinPriceMin = 5;
    [DataField] public int SpinPriceMax = 100;
    [DataField] public int SmallPrizeBase = 50;
    [DataField] public int BigPrizeBase = 150;
    [DataField] public float SpinTime = 1.5f;
    [DataField] public float ReelStopDelay = 0.7f;

    [DataField] public float NextSpinDelay = 2.4f;
    [DataField] public float BonusChance = 0.01f;
    [DataField] public int ScatterWinBase = 5;
    [DataField] public int MaxHistory = 5;

    [DataField]
    public Dictionary<string, int> SymbolWeights = new()
    {
        { "seven", 1 },
        { "cherry", 3 },
        { "banana", 3 },
        { "strawberry", 3 },
        { "grape", 3 },
        { "diamond", 3 },
        { "star", 3 },
        { "watermelon", 3 },
        { "wild", 1 },
        { "scatter", 1 },
    };

    /// <summary>Possible multipliers rolled when the bonus triggers.</summary>
    [DataField]
    public List<int> BonusMultipliers = new() { 2, 2, 2, 3, 3, 5 };

    /// <summary>Free spins awarded for a 3-in-row (rolled between min and max, times bonus).</summary>
    [DataField] public int FreeSpinsMin = 1;
    [DataField] public int FreeSpinsMax = 3;

    /// <summary>Chance that the red/black gamble doubles the prize.</summary>
    [DataField] public float GambleWinChance = 0.25f;

    /// <summary>Upper bound for the auto-spin counter.</summary>
    [DataField] public int AutoSpinMax = 50;

    /// <summary>After this many consecutive losses the bonus chance starts growing...</summary>
    [DataField] public int LossStreakHot = 5;

    /// <summary>...by this step per extra loss, capped at <see cref="BonusChanceMax"/>.</summary>
    [DataField] public float BonusStreakStep = 0.02f;
    [DataField] public float BonusChanceMax = 0.15f;

    /// <summary>Sevens/wilds on the middle line that trigger the near-miss message.</summary>
    [DataField] public int NearMissSevens = 3;

    /// <summary>Play the fuck sound on every Nth consecutive loss.</summary>
    [DataField] public int FuckSoundEvery = 7;

    [DataField] public SoundSpecifier SpinSound = new SoundPathSpecifier("/Audio/ADT/Machines/SlotMachine/Casino/wheel.ogg");
    [DataField] public SoundSpecifier MoneySound = new SoundPathSpecifier("/Audio/ADT/Machines/SlotMachine/Casino/money.ogg");
    [DataField] public SoundSpecifier BonusSound = new SoundPathSpecifier("/Audio/ADT/Machines/SlotMachine/Casino/bonus.ogg");
    [DataField] public SoundSpecifier JackpotSound = new SoundPathSpecifier("/Audio/ADT/Machines/SlotMachine/Casino/jackpot.ogg");
    [DataField] public SoundSpecifier CasinoFuckSound = new SoundPathSpecifier("/Audio/ADT/Machines/SlotMachine/Casino/casino_fuck.ogg");
    [DataField] public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/ADT/Machines/SlotMachine/Casino/money.ogg");
}

[Serializable, NetSerializable]
public readonly record struct SlotSpinHistoryEntry(string Result, string Message, int Bet, int Won);
