// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Addiction.Symptoms;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Держит на мобе симптомы ломки из AddictionPrototype.Stages[..].Symptoms.
///
/// Каналов в ломке может быть несколько, и их симптомы дерутся за один и тот же статус-эффект:
/// арбитраж по ключу симптома, побеждает пришедший с более тяжёлой стадии. Снимаем только то,
/// что наложили сами: чужую дрожь и чужие статус-эффекты ломка не гасит.
/// </summary>
public sealed class AddictionSymptomsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    /// <summary>
    /// Победители арбитража: ключ симптома -> стадия, с которой он пришёл, и сам симптом.
    /// </summary>
    private readonly Dictionary<string, (int Stage, AddictionSymptom Symptom)> _winners = new();

    private readonly List<string> _expired = new();

    // Продление измеряется десятками секунд; срочное приходит через AddictionSymptomsChangedEvent.
    private const float UpdateInterval = 1f;

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, AddictionSymptomsChangedEvent>(OnSymptomsChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;

        _accumulator = 0f;

        var query = EntityQueryEnumerator<AddictionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Симптомы живут по длительности и требуют продления, пока ломка идёт.
            var active = false;
            foreach (var channel in comp.Channels)
            {
                if (!channel.InWithdrawal)
                    continue;

                active = true;
                break;
            }

            if (!active)
            {
                if (comp.AppliedSymptoms.Count > 0)
                    RefreshSymptoms(uid, comp);

                continue;
            }

            if (_timing.CurTime < comp.NextSymptomRefresh)
                continue;

            RefreshSymptoms(uid, comp);
        }
    }

    private void OnSymptomsChanged(EntityUid uid, AddictionComponent comp, ref AddictionSymptomsChangedEvent args)
    {
        RefreshSymptoms(uid, comp);
    }

    /// <summary>
    /// Накладывает и продлевает нужные симптомы, снимает те, что больше никто не требует.
    /// </summary>
    private void RefreshSymptoms(EntityUid uid, AddictionComponent comp)
    {
        comp.NextSymptomRefresh = _timing.CurTime + comp.SymptomRefreshInterval;

        _winners.Clear();

        foreach (var channel in comp.Channels)
        {
            if (!channel.InWithdrawal || channel.Stage <= 0)
                continue;

            if (!_proto.TryIndex(channel.Kind, out var proto))
                continue;

            if (proto.GetStage(channel.Stage) is not { } stageData)
                continue;

            foreach (var symptom in stageData.Symptoms)
            {
                if (_winners.TryGetValue(symptom.Key, out var current) && current.Stage >= channel.Stage)
                    continue;

                _winners[symptom.Key] = (channel.Stage, symptom);
            }
        }

        // Снять то, что ломка больше не поддерживает
        _expired.Clear();
        foreach (var (key, _) in comp.AppliedSymptoms)
        {
            if (!_winners.ContainsKey(key))
                _expired.Add(key);
        }

        foreach (var key in _expired)
        {
            comp.AppliedSymptoms[key].Remove(new AddictionSymptomContext(uid, EntityManager, true));
            comp.AppliedSymptoms.Remove(key);
        }

        // Наложить или продлить актуальное
        foreach (var (key, winner) in _winners)
        {
            var owned = comp.AppliedSymptoms.ContainsKey(key);

            if (!winner.Symptom.Apply(new AddictionSymptomContext(uid, EntityManager, owned)))
                continue;

            comp.AppliedSymptoms[key] = winner.Symptom;
        }
    }
}
