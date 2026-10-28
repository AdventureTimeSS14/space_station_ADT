using Content.Shared.ADT.Casino;
using Content.Shared.Cargo.Components;
using Content.Shared.Chat;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Casino;

public sealed partial class SlotMachineCasinoSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public const string SymSeven = "seven";
    public const string SymWild = "wild";
    public const string SymScatter = "scatter";
    private int _globalPlays;
    private int _globalJackpots;
    private DateOnly _statsDay;
    private bool _statsInitialized;

    public int GlobalPlays => _globalPlays;
    public int GlobalJackpots => _globalJackpots;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlotMachineCasinoComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlotMachineCasinoComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SlotMachineCasinoComponent, BoundUIOpenedEvent>(OnUiOpened);

        SubscribeLocalEvent<SlotMachineCasinoComponent, SlotMachineCasinoActionMessage>(OnAction);
    }

    private void OnMapInit(Entity<SlotMachineCasinoComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (!_statsInitialized)
        {
            _globalJackpots = _random.Next(1, 5); // false hope
            _globalPlays = _random.Next(75, 201);
            _statsDay = DateOnly.FromDateTime(DateTime.UtcNow);
            _statsInitialized = true;
        }

        RollReels(ent.Comp);
        comp.Balance = 0;
        comp.BetAmount = comp.SpinPriceMin;
        comp.ActiveLines = GetActiveLines(comp);
        Dirty(ent);
    }

    private void OnUiOpened(Entity<SlotMachineCasinoComponent> ent, ref BoundUIOpenedEvent args)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today != _statsDay)
        {
            _statsDay = today;
            _globalPlays = 0;
        }

        _globalPlays++;
        PushState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<SlotMachineCasinoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_power.IsPowered(uid))
                continue;

            comp.MoneyRemainder += frameTime / 2f;
            if (comp.MoneyRemainder >= 1f)
            {
                var whole = (int)comp.MoneyRemainder;
                comp.MoneyRemainder -= whole;
                comp.Money += whole;
            }

            if (comp.ShowWinOverlay && now >= comp.WinBlinkEndTime)
            {
                comp.ShowWinOverlay = false;
                _appearance.SetData(uid, SlotMachineCasinoVisuals.WinBlink, false);
            }

            if (!comp.Working)
            {
                if (comp.NextAutoSpinTime > TimeSpan.Zero && now >= comp.NextAutoSpinTime)
                {
                    comp.NextAutoSpinTime = TimeSpan.Zero;
                    if (comp.FreeSpinsRemaining > 0 || comp.Balance >= comp.BetAmount)
                        TrySpin((uid, comp), null);
                }
                continue;
            }

            if (comp.NextReelToStop < comp.Reels.Length && now >= comp.NextReelStopTime)
            {
                comp.ReelSpinning[comp.NextReelToStop] = false;
                comp.StoppedReels++;
                _appearance.SetData(uid, SlotMachineCasinoVisuals.StoppedReels, comp.StoppedReels);
                comp.NextReelToStop++;
                comp.NextReelStopTime = now + TimeSpan.FromSeconds(comp.ReelStopDelay);

                if (comp.NextReelToStop >= comp.Reels.Length)
                    ResolveSpin((uid, comp), now);
                else
                    PushState((uid, comp));
            }
        }
    }

    // === Player actions ===

    private void OnAction(Entity<SlotMachineCasinoComponent> ent, ref SlotMachineCasinoActionMessage msg)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        switch (msg.Action)
        {
            case SlotMachineCasinoAction.Spin:
                TrySpin(ent, msg.Actor);
                break;
            case SlotMachineCasinoAction.SetBet:
                if (msg.Amount >= ent.Comp.SpinPriceMin && msg.Amount <= ent.Comp.SpinPriceMax)
                {
                    ent.Comp.BetAmount = msg.Amount;
                    ent.Comp.ActiveLines = GetActiveLines(ent.Comp);
                    PushState(ent);
                }
                break;
            case SlotMachineCasinoAction.AutoSpin:
                if (msg.Amount > 0 && msg.Amount <= ent.Comp.AutoSpinMax && !ent.Comp.Working)
                {
                    ent.Comp.AutoSpin = true;
                    ent.Comp.AutoSpinRemaining = msg.Amount;
                    TrySpin(ent, msg.Actor);
                }
                break;
            case SlotMachineCasinoAction.StopAuto:
                StopAuto(ent.Comp);
                PushState(ent);
                break;
            case SlotMachineCasinoAction.CashOut:
                CashOut(ent, msg.Actor);
                break;
            case SlotMachineCasinoAction.Gamble:
                DoGamble(ent, msg.Choice);
                break;
            case SlotMachineCasinoAction.Collect:
                ent.Comp.GambleAvailable = false;
                ent.Comp.GambleAmount = 0;
                PushState(ent);
                break;
        }
    }

    private void OnInteractUsing(Entity<SlotMachineCasinoComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<CashComponent>(args.Used, out _) || !TryComp<StackComponent>(args.Used, out var stack))
            return;

        args.Handled = true;
        var amount = _stack.GetCount(args.Used);
        ent.Comp.Balance += amount;
        PlayCasinoSound(ent, ent.Comp.InsertSound, -5f);
        if (args.User != default)
            _popup.PopupPredicted(Loc.GetString("slotmachine-casino-inserted", ("amount", amount)), ent.Owner, args.User);
        QueueDel(args.Used);
        PushState(ent);
    }

    // === Money out ===

    private void CashOut(Entity<SlotMachineCasinoComponent> ent, EntityUid? user)
    {
        var comp = ent.Comp;
        if (comp.Balance <= 0)
            return;

        var coords = Transform(ent.Owner).Coordinates;
        var cash = Spawn("SpaceCash", coords);
        _stack.SetCount(cash, comp.Balance);

        if (user != null)
            _hands.TryPickupAnyHand(user.Value, cash);

        comp.Balance = 0;
        comp.SessionWinnings = 0;
        comp.WinStreak = 0;
        comp.LossStreak = 0;
        StopAuto(comp);
        comp.FreeSpinsRemaining = 0;
        PushState(ent);
    }

    private void DoGamble(Entity<SlotMachineCasinoComponent> ent, string choice)
    {
        var comp = ent.Comp;
        if (!comp.GambleAvailable || comp.GambleAmount <= 0 || comp.Working)
            return;

        if (choice != "red" && choice != "black")
            return;

        if (comp.Balance < comp.GambleAmount)
        {
            comp.ResultMessage = Loc.GetString("slotmachine-casino-gamble-nofunds");
            comp.ResultType = "lose";
            comp.GambleAvailable = false;
            comp.GambleAmount = 0;
            PushState(ent);
            return;
        }

        comp.Balance -= comp.GambleAmount;
        if (_random.Prob(comp.GambleWinChance))
        {
            comp.SessionWinnings += comp.GambleAmount;
            comp.GambleAmount *= 2;
            comp.Balance += comp.GambleAmount;
            comp.ResultMessage = Loc.GetString("slotmachine-casino-gamble-win", ("amount", comp.GambleAmount));
            comp.ResultType = "win";
            PlayCasinoSound(ent, comp.MoneySound, -3f);
        }
        else
        {
            comp.SessionWinnings -= comp.GambleAmount;
            comp.ResultMessage = Loc.GetString("slotmachine-casino-gamble-lose", ("amount", comp.GambleAmount));
            comp.ResultType = "lose";
            comp.GambleAmount = 0;
            comp.GambleAvailable = false;
            comp.WinStreak = 0;
        }

        PushState(ent);
    }

    // === Helpers ===

    public void StopAuto(SlotMachineCasinoComponent comp)
    {
        comp.AutoSpin = false;
        comp.AutoSpinRemaining = 0;
        comp.NextAutoSpinTime = TimeSpan.Zero;
    }

    private void PlayCasinoSound(Entity<SlotMachineCasinoComponent> ent, SoundSpecifier sound, float volume = 0f)
    {
        _audio.PlayPvs(sound, ent.Owner, AudioParams.Default.WithVolume(volume).WithPitchScale(_random.NextFloat(0.9f, 1.15f)));
    }

    private void PushState(Entity<SlotMachineCasinoComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, SlotMachineCasinoUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, SlotMachineCasinoUiKey.Key, BuildState(ent));
    }

    private SlotMachineCasinoBoundUserInterfaceState BuildState(Entity<SlotMachineCasinoComponent> ent)
    {
        var comp = ent.Comp;
        var reels = new string[5][];
        var stopped = new bool[5];
        for (var i = 0; i < 5; i++)
        {
            reels[i] = (string[])comp.Reels[i].Clone();
            stopped[i] = !comp.ReelSpinning[i];
        }

        return new SlotMachineCasinoBoundUserInterfaceState
        {
            Money = comp.Money,
            Balance = comp.Balance,
            BetAmount = comp.BetAmount,
            Working = comp.Working,
            ActiveLines = comp.ActiveLines,
            WinningLine = comp.WinningLine,
            Plays = _globalPlays,
            SpinCount = comp.SpinCount,
            Jackpots = _globalJackpots,
            ResultMessage = comp.ResultMessage,
            ResultType = comp.ResultType,
            BonusMultiplier = comp.BonusMultiplier,
            BonusActive = comp.BonusActive,
            WinStreak = comp.WinStreak,
            LossStreak = comp.LossStreak,
            SessionWinnings = comp.SessionWinnings,
            AutoSpin = comp.AutoSpin,
            AutoSpinRemaining = comp.AutoSpinRemaining,
            FreeSpinsRemaining = comp.FreeSpinsRemaining,
            GambleAvailable = comp.GambleAvailable,
            GambleAmount = comp.GambleAmount,
            NearMiss = comp.NearMiss,
            NearMissMessage = comp.NearMissMessage,
            ShowConfetti = comp.ShowConfetti,
            Reels = reels,
            ReelsStopped = stopped,
            History = new List<SlotSpinHistoryEntry>(comp.SpinHistory),
        };
    }
}
