// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Fatigue;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Player;

namespace Content.Server.ADT.Fatigue;

/// <summary>
/// Серверная часть усталости: продвижение стадий, зевота, спотыкание, коллапс-сон, восстановление сном.
/// </summary>
public sealed partial class FatigueSystem : SharedFatigueSystem
{
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    /// <summary>Время последнего тика обработки усталости (троттлинг раз в секунду).</summary>
    private TimeSpan _lastUpdate;

    /// <summary>Истина после первого тика (чтобы не считать dt от нуля).</summary>
    private bool _hasLastUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled)
            return;

        // Троттлинг: обрабатываем раз в секунду, но dt считаем по реальному игровому времени,
        // чтобы при лагах прогресс стадий и восстановление сном не отставали.
        if (!_hasLastUpdate)
        {
            _lastUpdate = Timing.CurTime;
            _hasLastUpdate = true;
        }

        if (Timing.CurTime < _lastUpdate + TimeSpan.FromSeconds(1))
            return;
        var dt = Timing.CurTime - _lastUpdate;
        _lastUpdate = Timing.CurTime;

        var query = EntityQueryEnumerator<FatigueComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var fatigue, out var mobState))
        {
            if (mobState.CurrentState != MobState.Alive)
                continue;

            var ent = (uid, fatigue);

            // Добровольный сон (не коллапс): копит восстановление.
            if (HasComp<SleepingComponent>(uid) && !fatigue.FatigueForcedSleep)
            {
                TickVoluntaryRecovery(ent, dt);
                continue;
            }

            // Коллапс-сон: пробуждение по истечении таймера.
            if (HasComp<SleepingComponent>(uid) && fatigue.FatigueForcedSleep)
            {
                if (Timing.CurTime >= fatigue.NextStageAt)
                    EndCollapse(ent);
                continue;
            }

            // Бодрствование: продвижение стадий и эффекты.
            if (Timing.CurTime >= fatigue.NextStageAt)
                AdvanceStage(ent);

            if (fatigue.Stage >= 1 && fatigue.NextYawnAt != TimeSpan.Zero && Timing.CurTime >= fatigue.NextYawnAt)
                DoYawn(ent);

            if (fatigue.Stage >= 3 && fatigue.NextStumbleAt != TimeSpan.Zero && Timing.CurTime >= fatigue.NextStumbleAt)
                DoStumble(ent);
        }
    }

    private void AdvanceStage(Entity<FatigueComponent> ent)
    {
        var old = ent.Comp.Stage;
        if (old >= 4)
        {
            EndCollapse(ent);
            return;
        }

        ent.Comp.Stage = old + 1;
        ent.Comp.NextStageAt = Timing.CurTime + GetDurationForStage(ent, ent.Comp.Stage);
        ScheduleYawn(ent);
        if (ent.Comp.Stage >= 3)
            ScheduleStumble(ent);

        Dirty(ent);
        AfterStageChanged(ent, old);
        AnnounceStage(ent);

        if (ent.Comp.Stage >= 4)
            BeginCollapse(ent);
    }

    private void BeginCollapse(Entity<FatigueComponent> ent)
    {
        ent.Comp.FatigueForcedSleep = true;
        ent.Comp.NextStageAt = Timing.CurTime + ent.Comp.CollapseSleepDuration;
        Dirty(ent);

        Status.TrySetStatusEffectDuration(
            ent,
            SleepingSystem.StatusEffectForcedSleeping,
            ent.Comp.CollapseSleepDuration);

        var name = Identity.Name(ent, EntityManager);
        _popup.PopupEntity(
            Loc.GetString("fatigue-collapse", ("name", name)),
            ent,
            PopupType.MediumCaution);
    }

    private void EndCollapse(Entity<FatigueComponent> ent)
    {
        ClearFatigueSleep(ent);
        ResetToAlert(ent, rollNewTimer: true);

        var name = Identity.Name(ent, EntityManager);
        _popup.PopupEntity(
            Loc.GetString("fatigue-wake-rested", ("name", name)),
            ent,
            Filter.Pvs(ent),
            true,
            PopupType.Small);
    }

    private void TickVoluntaryRecovery(Entity<FatigueComponent> ent, TimeSpan dt)
    {
        if (ent.Comp.Stage <= 0)
        {
            // Уже бодр: сон просто отодвигает следующую стадию.
            ent.Comp.NextStageAt = Timing.CurTime + GetDurationForStage(ent, 0);
            Dirty(ent);
            return;
        }

        ent.Comp.SleepRecoveryAccumulated += dt;
        if (ent.Comp.SleepRecoveryAccumulated < ent.Comp.SleepRecoveryPerStage)
            return;

        ent.Comp.SleepRecoveryAccumulated = TimeSpan.Zero;
        var old = ent.Comp.Stage;

        if (old == 1)
        {
            // Сон снял последнюю стадию: ResetToAlert сам обновит скорость/алерт.
            ResetToAlert(ent, rollNewTimer: true);
            var name = Identity.Name(ent, EntityManager);
            _popup.PopupEntity(
                Loc.GetString("fatigue-recovered", ("name", name)),
                ent,
                ent,
                PopupType.Small);
            return;
        }

        ent.Comp.Stage = old - 1;
        ent.Comp.NextStageAt = Timing.CurTime + GetDurationForStage(ent, ent.Comp.Stage);
        ScheduleYawn(ent);
        Dirty(ent);
        AfterStageChanged(ent, old);
    }

    private void DoYawn(Entity<FatigueComponent> ent)
    {
        ScheduleYawn(ent);
        Dirty(ent);

        // forceEmote обходит только whitelist; ignoreActionBlocker обходит блокировку действий (сон и т.п.).
        _chat.TryEmoteWithChat(
            ent,
            "Yawn",
            range: ChatTransmitRange.Normal,
            hideLog: false,
            ignoreActionBlocker: true,
            forceEmote: true);

        if (ent.Comp.Stage >= 3)
        {
            var name = Identity.Name(ent, EntityManager);
            _popup.PopupEntity(
                Loc.GetString("fatigue-exhausted-emote", ("name", name)),
                ent,
                Filter.Pvs(ent),
                true,
                PopupType.Small);
        }
    }

    private void DoStumble(Entity<FatigueComponent> ent)
    {
        ScheduleStumble(ent);
        Dirty(ent);
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(1), refresh: true, autoStand: true, drop: false);
    }

    private void AnnounceStage(Entity<FatigueComponent> ent)
    {
        var name = Identity.Name(ent, EntityManager);
        var key = ent.Comp.Stage switch
        {
            1 => "fatigue-stage-1",
            2 => "fatigue-stage-2",
            3 => "fatigue-stage-3",
            4 => "fatigue-stage-4",
            _ => null,
        };

        if (key == null)
            return;

        // Стадии 1-3 зевотные: реальный эмоут (чат + звук), а не только попап.
        if (ent.Comp.Stage is >= 1 and <= 3)
        {
            _chat.TryEmoteWithChat(
                ent,
                "Yawn",
                range: ChatTransmitRange.Normal,
                hideLog: false,
                ignoreActionBlocker: true,
                forceEmote: true);
        }

        _popup.PopupEntity(
            Loc.GetString(key, ("name", name)),
            ent,
            Filter.Pvs(ent),
            true,
            PopupType.SmallCaution);
    }
}
