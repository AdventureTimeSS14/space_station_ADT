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
        if (args.NewMobState != MobState.Dead)
            return;

        ent.Comp.Queue.Clear();
    }

    public void Schedule(EntityUid boss, TimeSpan delay, Action action)
    {
        var sequence = EnsureComp<ADTMegafaunaSequenceComponent>(boss);
        sequence.Queue.Add(new ADTMegafaunaStep
        {
            ExecuteAt = _timing.CurTime + delay,
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
                if (TerminatingOrDeleted(uid) || _mobState.IsDead(uid))
                {
                    sequence.Queue.Clear();
                    break;
                }

                step.Execute();
            }
        }
    }
}
