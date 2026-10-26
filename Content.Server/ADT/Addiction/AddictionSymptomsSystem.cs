// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Addiction.Symptoms;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Держит на мобе симптомы ломки. Симптомы с одним ключом не складываются: побеждает
/// пришедший с более тяжёлой стадии.
/// </summary>
public sealed class AddictionSymptomsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly Dictionary<string, (int Stage, AddictionSymptom Symptom)> _winners = new();

    private readonly List<string> _expired = new();

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

        foreach (var (key, winner) in _winners)
        {
            var owned = comp.AppliedSymptoms.ContainsKey(key);

            if (!winner.Symptom.Apply(new AddictionSymptomContext(uid, EntityManager, owned)))
                continue;

            comp.AppliedSymptoms[key] = winner.Symptom;
        }
    }
}
