// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Traits;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Применяет симптомы ломки (дрожь, косноязычие, слабость, галлюцинации).
public sealed partial class AddictionSymptomsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

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

    private void RefreshSymptoms(EntityUid uid, AddictionComponent comp)
    {
        var anyWithdrawal = false;
        var maxStage = 0;
        var wantSlurred = false;
        var wantStutter = false;
        var wantWeakness = false;
        var wantMonochromacy = false;

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

            // Стадия 3 (тяжёлая): слабость, у наркотиков монохромный мир
            if (channel.Stage >= 3)
            {
                wantWeakness = true;
                if (channel.Kind == AddictionKind.Drug)
                    wantMonochromacy = true;
            }
        }

        // Дрожь
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

        if (wantMonochromacy)
        {
            if (!HasComp<MonochromacyComponent>(uid) && !comp.WithdrawalMonochromacyApplied)
            {
                AddComp<MonochromacyComponent>(uid);
                comp.WithdrawalMonochromacyApplied = true;
            }
        }
        else if (comp.WithdrawalMonochromacyApplied)
        {
            if (HasComp<MonochromacyComponent>(uid))
                RemComp<MonochromacyComponent>(uid);
            comp.WithdrawalMonochromacyApplied = false;
        }
    }
}
