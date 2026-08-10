// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Применяет симптомы ломки (дрожь, косноязычие, слабость, галлюцинации).
/// Слушает AddictionSymptomsChangedEvent от AddictionSystem и пересчитывает симптомы
/// по всем каналам: доза по одному каналу не снимает симптомы другого.
/// Продлевает симптомы по таймеру, пока идёт ломка.
/// </summary>
public sealed partial class AddictionSymptomsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Directed-подписка: событие рейзится на сущность (RaiseLocalEvent без broadcast),
        // broadcast-подписчики в этом форке такое не получают (см. грабли).
        SubscribeLocalEvent<AddictionComponent, AddictionSymptomsChangedEvent>(OnSymptomsChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AddictionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var anyWithdrawal = false;
            var due = false;
            foreach (var channel in comp.Channels)
            {
                if (!channel.InWithdrawal)
                    continue;

                anyWithdrawal = true;
                if (_timing.CurTime >= channel.NextSymptomsTime)
                    due = true;
            }

            if (!anyWithdrawal || !due)
                continue;

            RefreshSymptoms(uid, comp);

            foreach (var channel in comp.Channels)
            {
                if (channel.InWithdrawal)
                    channel.NextSymptomsTime = _timing.CurTime + comp.SymptomRefreshInterval;
            }
        }
    }

    private void OnSymptomsChanged(EntityUid uid, AddictionComponent comp, ref AddictionSymptomsChangedEvent args)
    {
        RefreshSymptoms(uid, comp);
    }

    /// <summary>
    /// Пересчитывает симптомы по всем каналам: применяет нужные, убирает лишние.
    /// Stutter/Slurred/Rainbow не снимает: эти эффекты могут висеть от других источников
    /// (алкоголь, ЛСД, THC), они истекают сами. Снимаются только свои: слабость
    /// (уникальный прототип) и дрожь.
    /// </summary>
    private void RefreshSymptoms(EntityUid uid, AddictionComponent comp)
    {
        var anyWithdrawal = false;
        var maxStage = 0;
        var wantSlurred = false;
        var wantStutter = false;
        var wantWeakness = false;
        var wantRainbow = false;

        foreach (var channel in comp.Channels)
        {
            if (!channel.InWithdrawal)
                continue;

            anyWithdrawal = true;
            maxStage = Math.Max(maxStage, channel.Stage);

            // Стадия 2 (средняя): косноязычие (алкоголь) или заикание (остальное)
            if (channel.Stage >= 2)
            {
                if (channel.Kind == AddictionKind.Alcohol)
                    wantSlurred = true;
                else
                    wantStutter = true;
            }

            // Стадия 3 (тяжёлая): слабость, у наркотиков галлюцинации
            if (channel.Stage >= 3)
            {
                wantWeakness = true;
                if (channel.Kind == AddictionKind.Drug)
                    wantRainbow = true;
            }
        }

        // Дрожь - косметика на любой стадии, амплитуда по самой тяжёлой.
        // refresh: true, чтобы время не копилось при повторных вызовах.
        if (anyWithdrawal)
        {
            var amplitude = maxStage switch
            {
                1 => comp.MildJitterAmplitude,
                2 => comp.MediumJitterAmplitude,
                _ => comp.SevereJitterAmplitude,
            };
            _jitter.DoJitter(uid, comp.SymptomDuration, refresh: true, amplitude, comp.JitterFrequency);
        }
        else
        {
            RemComp<JitteringComponent>(uid);
        }

        if (wantSlurred)
            _status.TrySetStatusEffectDuration(uid, comp.SlurredEffect, comp.SymptomDuration);

        if (wantStutter)
            _status.TrySetStatusEffectDuration(uid, comp.StutterEffect, comp.SymptomDuration);

        if (wantWeakness)
            _status.TrySetStatusEffectDuration(uid, comp.WeaknessEffect, comp.SymptomDuration);
        else
            _status.TryRemoveStatusEffect(uid, comp.WeaknessEffect);

        if (wantRainbow)
            _status.TrySetStatusEffectDuration(uid, comp.RainbowEffect, comp.SymptomDuration);
    }
}
