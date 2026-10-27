using Content.Shared.ADT.Casino;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Casino;

/// <summary>
/// Spin state machine and prize resolution for the casino slot machine.
/// </summary>
public sealed partial class SlotMachineCasinoSystem
{
    public bool TrySpin(Entity<SlotMachineCasinoComponent> ent, EntityUid? user)
    {
        var comp = ent.Comp;

        if (!_power.IsPowered(ent.Owner) || comp.Working)
        {
            StopAuto(comp);
            return false;
        }

        if (comp.FreeSpinsRemaining <= 0 && comp.Balance < comp.BetAmount)
        {
            if (user != null)
                _popup.PopupPredicted(Loc.GetString("slotmachine-casino-no-money"), ent.Owner, user.Value);
            StopAuto(comp);
            return false;
        }

        if (comp.FreeSpinsRemaining > 0)
        {
            comp.FreeSpinsRemaining--;
        }
        else
        {
            comp.Balance -= comp.BetAmount;
            comp.Money += comp.BetAmount;
        }

        comp.Working = true;
        comp.ResultMessage = string.Empty;
        comp.ResultType = string.Empty;
        comp.BonusMultiplier = 1;
        comp.BonusActive = false;
        comp.GambleAvailable = false;
        comp.GambleAmount = 0;
        comp.NearMiss = false;
        comp.NearMissMessage = string.Empty;
        comp.ShowConfetti = false;
        comp.ActiveLines = GetActiveLines(comp);
        comp.WinningLine = 0;
        comp.StoppedReels = 0;
        comp.NextReelToStop = 0;
        comp.SpinStartTime = _timing.CurTime;
        comp.NextReelStopTime = comp.SpinStartTime + TimeSpan.FromSeconds(comp.SpinTime);

        // The result is decided the moment the spin starts: roll the final symbols for
        // every reel now, so the client lands each reel on exactly these symbols.
        RollReels(comp);

        for (var i = 0; i < comp.Reels.Length; i++)
            comp.ReelSpinning[i] = true;

        _appearance.SetData(ent.Owner, SlotMachineCasinoVisuals.Spinning, true);
        _appearance.SetData(ent.Owner, SlotMachineCasinoVisuals.StoppedReels, 0);
        comp.SpinCount++;
        PlayCasinoSound(ent, comp.SpinSound, -8f);

        PushState(ent);
        return true;
    }

    private void RollReels(SlotMachineCasinoComponent comp)
    {
        for (var i = 0; i < comp.Reels.Length; i++)
        {
            comp.Reels[i] = new string[3];
            for (var row = 0; row < 3; row++)
                comp.Reels[i][row] = PickSymbol(comp);
        }
    }

    private void ResolveSpin(Entity<SlotMachineCasinoComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;
        comp.Working = false;
        _appearance.SetData(ent.Owner, SlotMachineCasinoVisuals.Spinning, false);
        GivePrizes(ent);

        if (comp.FreeSpinsRemaining > 0 || (comp.AutoSpin && comp.AutoSpinRemaining > 0))
        {
            if (comp.AutoSpin && comp.AutoSpinRemaining > 0)
            {
                comp.AutoSpinRemaining--;
                if (comp.AutoSpinRemaining <= 0)
                    StopAuto(comp);
            }
            comp.NextAutoSpinTime = now + TimeSpan.FromSeconds(comp.NextSpinDelay);
        }

        PushState(ent);
    }

    // === Prizes ===

    private void GivePrizes(Entity<SlotMachineCasinoComponent> ent)
    {
        var comp = ent.Comp;
        var (lineLength, winningLine) = GetLines(comp);
        comp.WinningLine = winningLine;

        var betMultiplier = comp.BetAmount / comp.SpinPriceMin;
        var wonAmount = 0;

        comp.NearMiss = false;
        comp.NearMissMessage = string.Empty;
        comp.ShowConfetti = false;
        comp.GambleAvailable = false;
        comp.GambleAmount = 0;

        var pairCount = CheckPairs(comp);
        var effectiveBonusChance = comp.BonusChance;
        if (comp.LossStreak >= comp.LossStreakHot)
            effectiveBonusChance = Math.Min(
                comp.BonusChance + (comp.LossStreak - comp.LossStreakHot + 1) * comp.BonusStreakStep,
                comp.BonusChanceMax);

        if (pairCount >= 2 && lineLength < 3 && _random.Prob(effectiveBonusChance))
        {
            comp.BonusMultiplier = _random.Pick(comp.BonusMultipliers);
            comp.BonusActive = true;
            PlayCasinoSound(ent, comp.BonusSound, -3f);
        }

        var jackpotMatch = true;
        for (var col = 0; col < 5; col++)
        {
            var sym = comp.Reels[col][1];
            if (sym != SymSeven && sym != SymWild)
            {
                jackpotMatch = false;
                break;
            }
        }

        if (jackpotMatch)
        {
            comp.WinningLine = 1;
            wonAmount = comp.Money;
            comp.Balance += wonAmount;
            comp.Money = 0;
            PlayCasinoSound(ent, comp.JackpotSound, 0f);
            _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString("slotmachine-casino-jackpot-speech", ("amount", wonAmount)), InGameICChatType.Speak, hideChat: true, hideLog: true, checkRadioPrefix: false);
            _chat.DispatchGlobalAnnouncement(Loc.GetString("slotmachine-casino-jackpot-announce", ("amount", wonAmount)), "Casino");
            _globalJackpots++;
            comp.ResultMessage = Loc.GetString("slotmachine-casino-result-jackpot", ("amount", wonAmount));
            RegisterWin(comp, "jackpot", wonAmount, confetti: true, allowGamble: false);
        }
        else if (lineLength == 5)
        {
            wonAmount = (int)MathF.Round(comp.BigPrizeBase * betMultiplier * comp.BonusMultiplier);
            PlayCasinoSound(ent, comp.MoneySound, -3f);
            _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString("slotmachine-casino-bigwin-speech", ("amount", wonAmount)), InGameICChatType.Speak, hideChat: true, hideLog: true, checkRadioPrefix: false);
            comp.ResultMessage = Loc.GetString("slotmachine-casino-result-bigwin", ("amount", wonAmount));
            if (comp.BonusActive)
                comp.ResultMessage += Loc.GetString("slotmachine-casino-result-bonus-append", ("multiplier", comp.BonusMultiplier));
            GiveMoney(comp, wonAmount);
            RegisterWin(comp, "win", wonAmount, confetti: true, allowGamble: true);
        }
        else if (lineLength == 4)
        {
            wonAmount = (int)MathF.Round(comp.SmallPrizeBase * betMultiplier * comp.BonusMultiplier);
            PlayCasinoSound(ent, comp.MoneySound, -4f);
            _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString("slotmachine-casino-win-speech", ("amount", wonAmount)), InGameICChatType.Speak, hideChat: true, hideLog: true, checkRadioPrefix: false);
            comp.ResultMessage = Loc.GetString("slotmachine-casino-result-win", ("amount", wonAmount));
            if (comp.BonusActive)
                comp.ResultMessage += Loc.GetString("slotmachine-casino-result-bonus-append", ("multiplier", comp.BonusMultiplier));
            GiveMoney(comp, wonAmount);
            RegisterWin(comp, "win", wonAmount, confetti: false, allowGamble: true);
        }
        else if (lineLength == 3)
        {
            var freeSpins = _random.Next(comp.FreeSpinsMin, comp.FreeSpinsMax + 1);
            if (comp.BonusActive)
                freeSpins *= comp.BonusMultiplier;
            comp.FreeSpinsRemaining += freeSpins;
            PlayCasinoSound(ent, comp.BonusSound, -4f);
            comp.ResultMessage = Loc.GetString("slotmachine-casino-result-freespins", ("amount", freeSpins));
            if (comp.BonusActive)
                comp.ResultMessage += Loc.GetString("slotmachine-casino-result-bonus-append", ("multiplier", comp.BonusMultiplier));
            RegisterWin(comp, "small", 0, confetti: false, allowGamble: false);
        }
        else if (comp.BonusActive)
        {
            wonAmount = comp.BetAmount * comp.BonusMultiplier;
            PlayCasinoSound(ent, comp.BonusSound, -3f);
            comp.ResultMessage = Loc.GetString("slotmachine-casino-result-bonus", ("multiplier", comp.BonusMultiplier), ("amount", wonAmount));
            GiveMoney(comp, wonAmount);
            RegisterWin(comp, "bonus", wonAmount, confetti: false, allowGamble: true);
        }
        else
        {
            comp.ResultType = "lose";
            comp.WinStreak = 0;
            comp.LossStreak++;
            comp.ResultMessage = comp.LossStreak >= comp.LossStreakHot
                ? Loc.GetString("slotmachine-casino-result-hot", ("streak", comp.LossStreak))
                : Loc.GetString("slotmachine-casino-result-lose");


            if (comp.LossStreak % comp.FuckSoundEvery == 0)
                PlayCasinoSound(ent, comp.CasinoFuckSound, -6f);
        }

        var scatterCount = CountScatters(comp);
        if (scatterCount >= 3)
        {
            var scatterWin = (int)MathF.Round(comp.ScatterWinBase * betMultiplier * scatterCount);
            wonAmount += scatterWin;
            if (comp.ResultType == "lose")
            {
                comp.ResultMessage = Loc.GetString("slotmachine-casino-result-scatter", ("count", scatterCount), ("amount", scatterWin));
                RegisterWin(comp, "win", scatterWin, confetti: false, allowGamble: true);
            }
            else
            {
                comp.ResultMessage += Loc.GetString("slotmachine-casino-result-scatter-append", ("amount", scatterWin));
                comp.SessionWinnings += scatterWin;
                GiveMoney(comp, scatterWin);
                comp.GambleAvailable = true;
                comp.GambleAmount += scatterWin;
            }

            PlayCasinoSound(ent, comp.BonusSound, -4f);
        }

        if (comp.ResultType == "lose")
            DetectNearMiss(comp);

        comp.SpinHistory.Add(new SlotSpinHistoryEntry(comp.ResultType, comp.ResultMessage, comp.BetAmount, wonAmount));
        if (comp.SpinHistory.Count > comp.MaxHistory)
            comp.SpinHistory.RemoveAt(0);

        if (comp.ResultType != "lose")
        {
            comp.ShowWinOverlay = true;
            comp.WinBlinkEndTime = _timing.CurTime + TimeSpan.FromSeconds(2.4f);
            _appearance.SetData(ent.Owner, SlotMachineCasinoVisuals.WinBlink, true);
        }
    }

    private void RegisterWin(SlotMachineCasinoComponent comp, string type, int amount, bool confetti, bool allowGamble)
    {
        comp.ResultType = type;
        comp.WinStreak++;
        comp.LossStreak = 0;
        comp.SessionWinnings += amount;

        if (confetti)
            comp.ShowConfetti = true;

        if (allowGamble)
        {
            comp.GambleAvailable = true;
            comp.GambleAmount += amount;
        }
    }

    private void GiveMoney(SlotMachineCasinoComponent comp, int amount)
    {
        comp.Money = Math.Max(0, comp.Money - amount);
        comp.Balance += amount;
    }

    private void DetectNearMiss(SlotMachineCasinoComponent comp)
    {
        var sevenCount = 0;
        for (var i = 0; i < 5; i++)
        {
            if (comp.Reels[i][1] == SymSeven || comp.Reels[i][1] == SymWild)
                sevenCount++;
        }

        if (sevenCount >= comp.NearMissSevens)
        {
            comp.NearMiss = true;
            comp.NearMissMessage = Loc.GetString("slotmachine-casino-nearmiss-sevens", ("count", sevenCount));
            return;
        }

        for (var li = 0; li < comp.ActiveLines; li++)
        {
            var payline = SlotMachineCasinoComponent.Paylines[li];
            var s1 = comp.Reels[0][payline[0]];
            var s2 = comp.Reels[1][payline[1]];
            var s3 = comp.Reels[2][payline[2]];
            if (s1 == SymScatter || s2 == SymScatter)
                continue;

            var match1 = s1 == SymWild ? s2 : s1;
            var match2 = s2 == SymWild ? s1 : s2;
            if (match1 == match2 || s1 == SymWild || s2 == SymWild)
            {
                if (s3 != match1 && s3 != SymWild)
                {
                    comp.NearMiss = true;
                    comp.NearMissMessage = Loc.GetString("slotmachine-casino-nearmiss-close");
                    return;
                }
            }
        }
    }

    // === Line math ===

    private int CheckPairs(SlotMachineCasinoComponent comp)
    {
        var counts = new Dictionary<string, int>();
        var wildCount = 0;
        for (var i = 0; i < 5; i++)
        {
            var sym = comp.Reels[i][1];
            if (sym == SymWild)
            {
                wildCount++;
                continue;
            }
            if (sym == SymScatter)
                continue;
            counts[sym] = counts.GetValueOrDefault(sym) + 1;
        }

        var best = 0;
        foreach (var (_, count) in counts)
            best = Math.Max(best, count + wildCount);
        if (counts.Count == 0 && wildCount > 0)
            best = wildCount;
        return best;
    }

    private int CountScatters(SlotMachineCasinoComponent comp)
    {
        var count = 0;
        for (var col = 0; col < 5; col++)
        {
            for (var row = 0; row < 3; row++)
            {
                if (comp.Reels[col][row] == SymScatter)
                {
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    private int GetActiveLines(SlotMachineCasinoComponent comp)
    {
        if (comp.BetAmount >= 25)
            return 3;
        if (comp.BetAmount >= 10)
            return 2;
        return 1;
    }

    private (int Length, int Line) GetLines(SlotMachineCasinoComponent comp)
    {
        var best = 0;
        var bestLine = 0;
        for (var lineIdx = 0; lineIdx < comp.ActiveLines; lineIdx++)
        {
            var payline = SlotMachineCasinoComponent.Paylines[lineIdx];
            string? targetSym = null;
            var run = 0;
            for (var pos = 0; pos < 5; pos++)
            {
                var sym = comp.Reels[pos][payline[pos]];
                if (sym == SymScatter)
                    break;
                if (sym == SymWild)
                {
                    run++;
                    continue;
                }
                if (targetSym == null)
                {
                    targetSym = sym;
                    run++;
                }
                else if (sym == targetSym)
                {
                    run++;
                }
                else
                {
                    break;
                }
            }

            if (run >= 3 && run > best)
            {
                best = run;
                bestLine = lineIdx + 1;
            }
        }

        return (best, bestLine);
    }

    private string PickSymbol(SlotMachineCasinoComponent comp)
    {
        var total = 0;
        foreach (var (_, weight) in comp.SymbolWeights)
            total += weight;
        if (total <= 0)
            return SymSeven;

        var roll = _random.Next(total);
        foreach (var (symbol, weight) in comp.SymbolWeights)
        {
            roll -= weight;
            if (roll < 0)
                return symbol;
        }
        return SymSeven;
    }
}
