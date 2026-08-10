// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.CCVar;
using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Fatigue;

/// <summary>
/// Биологическая усталость: стадии замедляют моба, размывают зрение и в итоге принудительно усыпляют.
/// Сон на стадии 4 использует <see cref="SleepingSystem.StatusEffectForcedSleeping"/>, чтобы пробуждение руками было заблокировано.
/// </summary>
public abstract partial class SharedFatigueSystem : EntitySystem
{
    [Dependency] protected IConfigurationManager Config = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected INetManager Net = default!;
    [Dependency] protected IRobustRandom Random = default!;
    [Dependency] protected MovementSpeedModifierSystem Movement = default!;
    [Dependency] protected StatusEffectsSystem Status = default!;
    [Dependency] protected BlurryVisionSystem Blurry = default!;
    [Dependency] protected SleepingSystem Sleeping = default!;
    [Dependency] protected AlertsSystem Alerts = default!;

    protected bool Enabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(Config, ADTCCVars.GameFatigueEnabled, v => Enabled = v, true);

        SubscribeLocalEvent<FatigueComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FatigueComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FatigueComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<FatigueComponent, GetBlurEvent>(OnGetBlur);
        SubscribeLocalEvent<FatigueComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<FatigueComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<FatigueComponent> ent, ref MapInitEvent args)
    {
        if (!Enabled || Net.IsClient)
            return;

        ResetToAlert(ent, rollNewTimer: true);
    }

    private void OnShutdown(Entity<FatigueComponent> ent, ref ComponentShutdown args)
    {
        Movement.RefreshMovementSpeedModifiers(ent);
        if (TryComp<BlindableComponent>(ent, out var blindable))
            Blurry.UpdateBlurMagnitude((ent, blindable));
        Alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnRefreshMovespeed(Entity<FatigueComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!Enabled)
            return;

        var mod = GetSpeedModifier(ent.Comp);
        if (mod < 1f)
            args.ModifySpeed(mod);
    }

    private void OnGetBlur(Entity<FatigueComponent> ent, ref GetBlurEvent args)
    {
        if (!Enabled || ent.Comp.Stage < 3)
            return;

        args.Blur += ent.Comp.Stage3Blur;
    }

    private void OnRejuvenate(Entity<FatigueComponent> ent, ref RejuvenateEvent args)
    {
        if (Net.IsClient)
            return;

        ClearFatigueSleep(ent);
        ResetToAlert(ent, rollNewTimer: true);
    }

    private void OnMobStateChanged(Entity<FatigueComponent> ent, ref MobStateChangedEvent args)
    {
        if (Net.IsClient)
            return;

        // Мёртвым коллапс-сон не нужен; таймеры в серверном Update и так пропускают не-Alive.
        if (args.NewMobState is MobState.Dead)
        {
            ent.Comp.FatigueForcedSleep = false;
            Dirty(ent);
        }
    }

    /// <summary>
    /// Множитель скорости для текущей стадии.
    /// </summary>
    public float GetSpeedModifier(FatigueComponent comp)
    {
        return comp.Stage switch
        {
            1 => comp.Stage1SpeedModifier,
            2 => comp.Stage2SpeedModifier,
            >= 3 => comp.Stage3SpeedModifier,
            _ => 1f,
        };
    }

    /// <summary>
    /// Сброс на стадию 0 (бодр). При rollNewTimer заводится новый случайный таймер до первой стадии.
    /// </summary>
    public void ResetToAlert(Entity<FatigueComponent> ent, bool rollNewTimer)
    {
        var old = ent.Comp.Stage;
        ent.Comp.Stage = 0;
        ent.Comp.SleepRecoveryAccumulated = TimeSpan.Zero;
        ent.Comp.FatigueForcedSleep = false;
        ent.Comp.NextYawnAt = TimeSpan.Zero;
        ent.Comp.NextStumbleAt = TimeSpan.Zero;

        if (rollNewTimer)
        {
            ent.Comp.NextStageAt = Timing.CurTime + GetDurationForStage(ent, 0);
        }
        else
        {
            ent.Comp.NextStageAt = Timing.CurTime + GetDurationForStage(ent, 0);
        }

        Dirty(ent);
        if (old != 0)
            AfterStageChanged(ent, old);
    }

    /// <summary>
    /// Сдвиг стадии усталости. Отрицательная дельта бодрит, отрицательная дельта будит от коллапса.
    /// </summary>
    public void AdjustStage(EntityUid uid, int delta, FatigueComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false) || !Enabled)
            return;

        AdjustStage((uid, comp), delta);
    }

    /// <summary>
    /// Сдвиг стадии усталости.
    /// </summary>
    public void AdjustStage(Entity<FatigueComponent> ent, int delta)
    {
        if (!Enabled)
            return;

        if (delta == 0)
            return;

        // Бодрящий эффект выводит из коллапс-сна.
        if (delta < 0 && ent.Comp.FatigueForcedSleep)
            ClearFatigueSleep(ent);

        var old = ent.Comp.Stage;
        var next = Math.Clamp(ent.Comp.Stage + delta, 0, 4);
        if (next == old)
        {
            if (next == 0)
                ResetToAlert(ent, rollNewTimer: true);
            return;
        }

        if (next == 0)
        {
            // ResetToAlert сам поставит стадию 0 и обновит скорость/алерт (old != 0).
            ResetToAlert(ent, rollNewTimer: true);
            return;
        }

        ent.Comp.Stage = next;
        ent.Comp.NextStageAt = Timing.CurTime + GetDurationForStage(ent, next);
        ScheduleYawn(ent);
        if (next >= 3)
            ScheduleStumble(ent);

        Dirty(ent);
        AfterStageChanged(ent, old);
    }

    /// <summary>
    /// Установка конкретной стадии.
    /// </summary>
    public void SetStage(EntityUid uid, int stage, FatigueComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        AdjustStage((uid, comp), stage - comp.Stage);
    }

    /// <summary>
    /// Длительность стадии. Для стадии 0 - случайный интервал бодрствования.
    /// Учитывает трайты «Бодрость»/«Сонливый» (множитель длительности).
    /// </summary>
    protected TimeSpan GetDurationForStage(Entity<FatigueComponent> ent, int stage)
    {
        var duration = stage switch
        {
            0 => TimeSpan.FromSeconds(Random.NextFloat(
                (float)ent.Comp.MinAlertDuration.TotalSeconds,
                (float)ent.Comp.MaxAlertDuration.TotalSeconds)),
            1 => ent.Comp.Stage1Duration,
            2 => ent.Comp.Stage2Duration,
            3 => ent.Comp.Stage3Duration,
            _ => ent.Comp.CollapseSleepDuration,
        };

        return duration * GetStageDurationMultiplier(ent);
    }

    /// <summary>
    /// Множитель длительности стадий от трайтов: «Бодрость» удлиняет, «Сонливый» укорачивает.
    /// </summary>
    private float GetStageDurationMultiplier(Entity<FatigueComponent> ent)
    {
        if (TryComp<EnergeticFatigueTraitComponent>(ent, out var energetic))
            return energetic.StageDurationMultiplier;

        if (TryComp<SleepyFatigueTraitComponent>(ent, out var sleepy))
            return sleepy.StageDurationMultiplier;

        return 1f;
    }

    /// <summary>
    /// Планирование следующей зевоты по текущей стадии.
    /// </summary>
    protected void ScheduleYawn(Entity<FatigueComponent> ent)
    {
        var (min, max) = ent.Comp.Stage switch
        {
            1 => (ent.Comp.Stage1YawnMin, ent.Comp.Stage1YawnMax),
            2 => (ent.Comp.Stage2YawnMin, ent.Comp.Stage2YawnMax),
            >= 3 => (ent.Comp.Stage3YawnMin, ent.Comp.Stage3YawnMax),
            _ => (0f, 0f),
        };

        if (max <= 0f)
        {
            ent.Comp.NextYawnAt = TimeSpan.Zero;
            return;
        }

        ent.Comp.NextYawnAt = Timing.CurTime + TimeSpan.FromSeconds(Random.NextFloat(min, max));
    }

    /// <summary>
    /// Планирование следующего спотыкания (стадия 3+).
    /// </summary>
    protected void ScheduleStumble(Entity<FatigueComponent> ent)
    {
        ent.Comp.NextStumbleAt = Timing.CurTime + TimeSpan.FromSeconds(
            Random.NextFloat(ent.Comp.Stage3StumbleMin, ent.Comp.Stage3StumbleMax));
    }

    /// <summary>
    /// Обновление скорости, размытия и HUD-алерта после смены стадии.
    /// </summary>
    protected void AfterStageChanged(Entity<FatigueComponent> ent, int oldStage)
    {
        Movement.RefreshMovementSpeedModifiers(ent);
        if (TryComp<BlindableComponent>(ent, out var blindable))
            Blurry.UpdateBlurMagnitude((ent, blindable));

        if (ent.Comp.Stage > 0)
            Alerts.ShowAlert(ent.Owner, ent.Comp.Alert, (short)ent.Comp.Stage);
        else
            Alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    /// <summary>
    /// Принудительное снятие коллапс-сна и пробуждение моба.
    /// </summary>
    public void ClearFatigueSleep(Entity<FatigueComponent> ent)
    {
        if (!ent.Comp.FatigueForcedSleep && !Status.HasEffectComp<ForcedSleepingStatusEffectComponent>(ent))
            return;

        ent.Comp.FatigueForcedSleep = false;
        Status.TryRemoveStatusEffect(ent, SleepingSystem.StatusEffectForcedSleeping);
        Sleeping.TryWaking(ent.Owner, force: true);
        Dirty(ent);
    }
}
