using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Drowsiness;

/// <summary>
/// ADT-Tweak OPTIMIZATION
/// Manages drowsiness status effect - random sleep incidents.
/// Shared for potential client-side prediction.
/// </summary>
public class SharedDrowsinessSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Note: Event subscriptions for StatusEffectAppliedEvent should be handled
        // in Server/Client subclasses to avoid duplicate subscriptions
    }

    protected virtual void OnEffectApplied(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.TimeBetweenIncidents.X, ent.Comp.TimeBetweenIncidents.Y));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DrowsinessStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var drowsiness, out var statusEffect))
        {
            if (_timing.CurTime < drowsiness.NextIncidentTime)
                continue;

            if (statusEffect.AppliedTo is null)
                continue;

            drowsiness.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(drowsiness.TimeBetweenIncidents.X, drowsiness.TimeBetweenIncidents.Y));

            var duration = TimeSpan.FromSeconds(_random.NextFloat(drowsiness.DurationOfIncident.X, drowsiness.DurationOfIncident.Y));
            drowsiness.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffectDuration(statusEffect.AppliedTo.Value, SleepingSystem.StatusEffectForcedSleeping, duration);
        }
    }
}
