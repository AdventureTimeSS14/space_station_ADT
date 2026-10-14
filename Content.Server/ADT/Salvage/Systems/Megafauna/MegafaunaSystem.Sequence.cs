using System.Linq;
using Content.Shared.Mobs;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    private readonly List<ADTMegafaunaStep> _dueSteps = new();

    private readonly List<Entity<ADTMegafaunaSequenceComponent>> _activeSequences = new();

    private void InitializeSequence()
    {
        SubscribeLocalEvent<ADTMegafaunaSequenceComponent, MobStateChangedEvent>(OnSequenceMobStateChanged);
    }

    private void OnSequenceMobStateChanged(Entity<ADTMegafaunaSequenceComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || ent.Comp.SurvivesDeath)
            return;

        ent.Comp.Queue.Clear();
    }

    public void Schedule(EntityUid boss, TimeSpan delay, Action action)
    {
        ScheduleAt(boss, _timing.CurTime + delay, action);
    }

    public void ScheduleAt(EntityUid boss, TimeSpan at, Action action)
    {
        var sequence = EnsureComp<ADTMegafaunaSequenceComponent>(boss);
        sequence.Queue.Add(new ADTMegafaunaStep
        {
            ExecuteAt = at,
            Execute = action,
        });
    }

    public void ClearSchedule(EntityUid boss)
    {
        if (TryComp<ADTMegafaunaSequenceComponent>(boss, out var sequence))
            sequence.Queue.Clear();
    }

    private void UpdateSequences(TimeSpan now)
    {
        _activeSequences.Clear();

        var query = EntityQueryEnumerator<ADTMegafaunaSequenceComponent>();

        while (query.MoveNext(out var uid, out var sequence))
        {
            if (sequence.Queue.Count > 0)
                _activeSequences.Add((uid, sequence));
        }

        foreach (var (uid, sequence) in _activeSequences)
        {
            if (sequence.InsertionOrder)
            {
                RunInInsertionOrder(uid, sequence, now);
                continue;
            }

            _dueSteps.Clear();

            for (var i = 0; i < sequence.Queue.Count; i++)
            {
                var step = sequence.Queue[i];

                if (now < step.ExecuteAt)
                    continue;

                _dueSteps.Add(step);
                sequence.Queue.RemoveAt(i);
                i--;
            }

            foreach (var step in _dueSteps.OrderBy(s => s.ExecuteAt).ToList())
            {
                if (TerminatingOrDeleted(uid) || !sequence.SurvivesDeath && _mobState.IsDead(uid))
                {
                    sequence.Queue.Clear();
                    break;
                }

                step.Execute();
            }
        }
    }

    private void RunInInsertionOrder(EntityUid uid, ADTMegafaunaSequenceComponent sequence, TimeSpan now)
    {
        for (var i = 0; i < sequence.Queue.Count; i++)
        {
            var step = sequence.Queue[i];

            if (now < step.ExecuteAt)
                continue;

            if (TerminatingOrDeleted(uid) || !sequence.SurvivesDeath && _mobState.IsDead(uid))
            {
                sequence.Queue.Clear();
                return;
            }

            sequence.Queue.RemoveAt(i);
            i--;

            step.Execute();
        }
    }
}
